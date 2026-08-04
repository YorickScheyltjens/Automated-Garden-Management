namespace GardenSystem.TelemetryConsumer.Messaging;

public interface ITelemetryMessagingClient : IAsyncDisposable
{
    Task ConnectAsync(Func<TelemetryMessage, Task> onTelemetryReceived, CancellationToken cancellationToken);

    Task PublishIrrigationCommandAsync(Guid plantId, CancellationToken cancellationToken);
}
