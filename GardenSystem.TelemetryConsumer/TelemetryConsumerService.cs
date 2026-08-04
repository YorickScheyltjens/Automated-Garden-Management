using GardenSystem.Application.Repositories;
using GardenSystem.Domain;
using GardenSystem.Domain.Entities;
using GardenSystem.TelemetryConsumer.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace GardenSystem.TelemetryConsumer;

public sealed class TelemetryConsumerService(
    ITelemetryMessagingClient messagingClient,
    IServiceScopeFactory scopeFactory,
    ILogger<TelemetryConsumerService> logger) : BackgroundService
{
    private const int ConnectRetryDelaySeconds = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectWithRetryAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
    }

    private async Task ConnectWithRetryAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                await messagingClient.ConnectAsync(
                    message => HandleTelemetryAsync(message, stoppingToken),
                    stoppingToken);
                return;
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    exception,
                    "Failed to connect to RabbitMQ, retrying in {RetryDelaySeconds} seconds.",
                    ConnectRetryDelaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(ConnectRetryDelaySeconds), stoppingToken);
            }
        }
    }

    private async Task HandleTelemetryAsync(TelemetryMessage message, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var plantRepository = scope.ServiceProvider.GetRequiredService<IPlantRepository>();
        var plantStateRepository = scope.ServiceProvider.GetRequiredService<IPlantStateRepository>();

        var plant = await plantRepository.GetByIdAsync(message.PlantId, cancellationToken);
        if (plant is null)
        {
            logger.LogWarning("Received telemetry for unknown plant {PlantId}.", message.PlantId);
            return;
        }

        var plantState = await plantStateRepository.GetByPlantIdAsync(message.PlantId, cancellationToken);
        var isNewPlantState = plantState is null;
        plantState ??= new PlantState { PlantId = message.PlantId };

        var wasIrrigating = plantState.IsCurrentlyIrrigating;
        plantState.CurrentHumidityLevel = message.CurrentHumidityLevel;
        plantState.UpdatedAtUtc = DateTime.UtcNow;

        if (wasIrrigating && !message.IsCurrentlyIrrigating)
        {
            plantState.IsCurrentlyIrrigating = false;
            plantState.LastIrrigationEndTime = DateTime.UtcNow;

            logger.LogInformation(
                "Plant {PlantId} finished irrigating, humidity now {CurrentHumidity}%.",
                message.PlantId,
                message.CurrentHumidityLevel);
        }
        else if (!wasIrrigating && TelemetryEvaluator.ShouldStartWatering(message.CurrentHumidityLevel, plant.IdealHumidityLevel, wasIrrigating))
        {
            await messagingClient.PublishIrrigationCommandAsync(message.PlantId, cancellationToken);

            plantState.IsCurrentlyIrrigating = true;
            plantState.LastIrrigationStartTime = DateTime.UtcNow;

            logger.LogInformation(
                "Plant {PlantId} humidity is {CurrentHumidity}%, below ideal {IdealHumidity}% - irrigation command sent.",
                message.PlantId,
                message.CurrentHumidityLevel,
                plant.IdealHumidityLevel);
        }

        if (isNewPlantState)
        {
            await plantStateRepository.AddAsync(plantState, cancellationToken);
        }
        else
        {
            await plantStateRepository.UpdateAsync(plantState, cancellationToken);
        }
    }
}
