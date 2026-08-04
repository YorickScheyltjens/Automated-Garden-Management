namespace GardenSystem.TelemetryConsumer.Configuration;

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5673;
    public string Username { get; set; } = "gardensystem";
    public string Password { get; set; } = "gardensystem";
}
