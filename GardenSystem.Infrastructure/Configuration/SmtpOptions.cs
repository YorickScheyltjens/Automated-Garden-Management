namespace GardenSystem.Infrastructure.Configuration;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
}
