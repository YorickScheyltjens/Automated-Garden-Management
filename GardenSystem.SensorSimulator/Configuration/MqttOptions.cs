namespace GardenSystem.SensorSimulator.Configuration;

public sealed class MqttOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1884;
    public string Username { get; set; } = "gardensystem";
    public string Password { get; set; } = "gardensystem";
}
