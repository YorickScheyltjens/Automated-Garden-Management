namespace GardenSystem.TelemetryConsumer.Messaging;

public static class AmqpTopics
{
    public const string TopicExchange = "amq.topic";
    public const string TelemetryQueueName = "telemetry-consumer.sensors-telemetry";
    public const string TelemetryBindingPattern = "sensors.*.telemetry";

    public static string IrrigationCommandRoutingKey(Guid plantId) => $"irrigation.{plantId}.commands";
}
