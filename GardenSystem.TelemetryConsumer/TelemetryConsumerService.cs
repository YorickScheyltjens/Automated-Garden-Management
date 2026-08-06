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
    private static readonly TimeSpan FlushInterval = TimeSpan.FromMilliseconds(500);

    private readonly SemaphoreSlim _pendingPlantStatesLock = new(1, 1);
    private readonly Dictionary<Guid, PendingPlantState> _pendingPlantStates = new();

    private sealed record PendingPlantState(PlantState PlantState, bool IsNew);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectWithRetryAsync(stoppingToken);
        await RunFlushLoopAsync(stoppingToken);
    }

    private async Task RunFlushLoopAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(FlushInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await FlushPendingPlantStatesAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
        finally
        {
            // Best-effort final flush so a graceful shutdown doesn't drop the last batch.
            await FlushPendingPlantStatesAsync(CancellationToken.None);
        }
    }

    private async Task FlushPendingPlantStatesAsync(CancellationToken cancellationToken)
    {
        Dictionary<Guid, PendingPlantState> toFlush;

        await _pendingPlantStatesLock.WaitAsync(cancellationToken);
        try
        {
            if (_pendingPlantStates.Count == 0)
            {
                return;
            }

            toFlush = new Dictionary<Guid, PendingPlantState>(_pendingPlantStates);
            _pendingPlantStates.Clear();
        }
        finally
        {
            _pendingPlantStatesLock.Release();
        }

        using var scope = scopeFactory.CreateScope();
        var plantStateRepository = scope.ServiceProvider.GetRequiredService<IPlantStateRepository>();

        foreach (var pending in toFlush.Values)
        {
            if (pending.IsNew)
            {
                await plantStateRepository.AddAsync(pending.PlantState, cancellationToken);
            }
            else
            {
                await plantStateRepository.UpdateAsync(pending.PlantState, cancellationToken);
            }
        }

        logger.LogInformation("Flushed {Count} pending plant state write(s) to the database.", toFlush.Count);
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
        var irrigationEventRepository = scope.ServiceProvider.GetRequiredService<IIrrigationEventRepository>();

        var plant = await plantRepository.GetByIdAsync(message.PlantId, cancellationToken);
        if (plant is null)
        {
            logger.LogWarning("Received telemetry for unknown plant {PlantId}.", message.PlantId);
            return;
        }

        await _pendingPlantStatesLock.WaitAsync(cancellationToken);
        try
        {
            PlantState plantState;
            bool isNewPlantState;

            if (_pendingPlantStates.TryGetValue(message.PlantId, out var pending))
            {
                plantState = pending.PlantState;
                isNewPlantState = pending.IsNew;
            }
            else
            {
                var existingPlantState = await plantStateRepository.GetByPlantIdAsync(message.PlantId, cancellationToken);
                isNewPlantState = existingPlantState is null;
                plantState = existingPlantState ?? new PlantState { PlantId = message.PlantId };
            }

            var wasIrrigating = plantState.IsCurrentlyIrrigating;
            plantState.CurrentHumidityLevel = message.CurrentHumidityLevel;
            plantState.UpdatedAtUtc = DateTime.UtcNow;

            if (wasIrrigating && !message.IsCurrentlyIrrigating)
            {
                plantState.IsCurrentlyIrrigating = false;
                plantState.LastIrrigationEndTime = DateTime.UtcNow;

                var openIrrigationEvent = await irrigationEventRepository.GetOpenEventByPlantIdAsync(message.PlantId, cancellationToken);
                if (openIrrigationEvent is not null)
                {
                    openIrrigationEvent.EndTimeUtc = DateTime.UtcNow;
                    openIrrigationEvent.HumidityAfter = message.CurrentHumidityLevel;
                    await irrigationEventRepository.UpdateAsync(openIrrigationEvent, cancellationToken);
                }

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

                await irrigationEventRepository.AddAsync(
                    new IrrigationEvent
                    {
                        IrrigationEventId = Guid.NewGuid(),
                        PlantId = message.PlantId,
                        StartTimeUtc = DateTime.UtcNow,
                        EndTimeUtc = null,
                        HumidityBefore = message.CurrentHumidityLevel,
                        HumidityAfter = null
                    },
                    cancellationToken);

                logger.LogInformation(
                    "Plant {PlantId} humidity is {CurrentHumidity}%, below ideal {IdealHumidity}% - irrigation command sent.",
                    message.PlantId,
                    message.CurrentHumidityLevel,
                    plant.IdealHumidityLevel);
            }

            _pendingPlantStates[message.PlantId] = new PendingPlantState(plantState, isNewPlantState);
        }
        finally
        {
            _pendingPlantStatesLock.Release();
        }
    }
}
