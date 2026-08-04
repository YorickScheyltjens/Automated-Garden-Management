using System.Collections.Concurrent;
using GardenSystem.SensorSimulator.Configuration;
using GardenSystem.SensorSimulator.Mqtt;
using GardenSystem.SensorSimulator.PlantRoster;
using GardenSystem.SensorSimulator.Simulation;
using Microsoft.Extensions.Options;

namespace GardenSystem.SensorSimulator;

public sealed class SensorSimulatorWorker(
    IPlantRosterClient rosterClient,
    IIrrigationMqttClient mqttClient,
    IOptions<SimulationOptions> simulationOptions,
    ILogger<SensorSimulatorWorker> logger) : BackgroundService
{
    private const decimal InitialHumidityLevel = 50m;
    private const int MqttConnectRetryDelaySeconds = 5;

    private readonly ConcurrentDictionary<Guid, PlantSimulationState> _plants = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectToMqttBrokerWithRetryAsync(stoppingToken);

        await RefreshRosterAsync(stoppingToken);

        var options = simulationOptions.Value;
        using var tickTimer = new PeriodicTimer(TimeSpan.FromSeconds(options.TickIntervalSeconds));
        using var rosterTimer = new PeriodicTimer(TimeSpan.FromSeconds(options.RosterPollIntervalSeconds));

        var rosterRefreshTask = RunRosterRefreshLoopAsync(rosterTimer, stoppingToken);

        while (await tickTimer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var plant in _plants.Values)
            {
                plant.Tick();

                await PublishReadingAsync(plant, stoppingToken);

                logger.LogInformation(
                    "Plant {PlantId} ({PlantType}) humidity is now {Humidity}% (irrigating: {IsIrrigating}).",
                    plant.PlantId,
                    plant.PlantType,
                    plant.CurrentHumidityLevel,
                    plant.IsCurrentlyIrrigating);
            }
        }

        await rosterRefreshTask;
    }

    private async Task ConnectToMqttBrokerWithRetryAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                await mqttClient.ConnectAsync(OnIrrigationCommandReceivedAsync, stoppingToken);
                return;
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    exception,
                    "Failed to connect to the MQTT broker, retrying in {RetryDelaySeconds} seconds.",
                    MqttConnectRetryDelaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(MqttConnectRetryDelaySeconds), stoppingToken);
            }
        }
    }

    private async Task RunRosterRefreshLoopAsync(PeriodicTimer rosterTimer, CancellationToken stoppingToken)
    {
        while (await rosterTimer.WaitForNextTickAsync(stoppingToken))
        {
            await RefreshRosterAsync(stoppingToken);
        }
    }

    private async Task RefreshRosterAsync(CancellationToken cancellationToken)
    {
        try
        {
            var roster = await rosterClient.GetPlantRosterAsync(cancellationToken);

            foreach (var entry in roster)
            {
                if (_plants.ContainsKey(entry.PlantId))
                {
                    continue;
                }

                var plant = new PlantSimulationState(entry.PlantId, entry.PlantType, InitialHumidityLevel);
                _plants[entry.PlantId] = plant;

                await PublishReadingAsync(plant, cancellationToken);

                logger.LogInformation(
                    "Discovered plant {PlantId} ({PlantType}), starting simulation at {Humidity}% humidity.",
                    plant.PlantId,
                    plant.PlantType,
                    plant.CurrentHumidityLevel);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Failed to poll the plant roster from the Api.");
        }
    }

    private Task PublishReadingAsync(PlantSimulationState plant, CancellationToken cancellationToken)
    {
        var reading = new TelemetryReading(plant.PlantId, plant.CurrentHumidityLevel, plant.IsCurrentlyIrrigating, DateTime.UtcNow);
        return mqttClient.PublishTelemetryAsync(reading, cancellationToken);
    }

    private Task OnIrrigationCommandReceivedAsync(Guid plantId)
    {
        if (_plants.TryGetValue(plantId, out var plant))
        {
            plant.StartIrrigating();
        }

        return Task.CompletedTask;
    }
}
