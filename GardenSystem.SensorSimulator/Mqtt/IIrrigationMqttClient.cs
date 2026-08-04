namespace GardenSystem.SensorSimulator.Mqtt;

public interface IIrrigationMqttClient : IAsyncDisposable
{
    Task ConnectAsync(Func<Guid, Task> onIrrigationCommandReceived, CancellationToken cancellationToken);

    Task PublishTelemetryAsync(TelemetryReading reading, CancellationToken cancellationToken);
}
