namespace GardenSystem.SensorSimulator.Mqtt;

public static class MqttTopics
{
    public const string CommandsTopicFilter = "irrigation/+/commands";

    public static string Telemetry(Guid plantId) => $"sensors/{plantId}/telemetry";

    public static bool TryGetPlantIdFromCommandTopic(string topic, out Guid plantId)
    {
        var segments = topic.Split('/');

        if (segments.Length == 3
            && segments[0] == "irrigation"
            && segments[2] == "commands"
            && Guid.TryParse(segments[1], out plantId))
        {
            return true;
        }

        plantId = Guid.Empty;
        return false;
    }
}
