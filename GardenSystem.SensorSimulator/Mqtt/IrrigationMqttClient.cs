using System.Text.Json;
using GardenSystem.SensorSimulator.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;

namespace GardenSystem.SensorSimulator.Mqtt;

public sealed class IrrigationMqttClient(
    IOptions<MqttOptions> mqttOptions,
    ILogger<IrrigationMqttClient> logger,
    MqttClientFactory factory) : IIrrigationMqttClient
{
    private readonly IMqttClient _client = factory.CreateMqttClient();

    public async Task ConnectAsync(Func<Guid, Task> onIrrigationCommandReceived, CancellationToken cancellationToken)
    {
        _client.ApplicationMessageReceivedAsync += async e =>
        {
            if (MqttTopics.TryGetPlantIdFromCommandTopic(e.ApplicationMessage.Topic, out var plantId))
            {
                logger.LogInformation("Received irrigation command for plant {PlantId}.", plantId);
                await onIrrigationCommandReceived(plantId);
            }
        };

        var options = mqttOptions.Value;
        var clientOptions = factory.CreateClientOptionsBuilder()
            .WithTcpServer(options.Host, options.Port)
            .WithCredentials(options.Username, options.Password)
            .WithClientId($"sensor-simulator-{Guid.NewGuid()}")
            .WithCleanSession(true)
            .Build();

        await _client.ConnectAsync(clientOptions, cancellationToken);

        var subscribeOptions = factory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(filter => filter.WithTopic(MqttTopics.CommandsTopicFilter))
            .Build();

        await _client.SubscribeAsync(subscribeOptions, cancellationToken);

        logger.LogInformation(
            "Connected to MQTT broker at {Host}:{Port} and subscribed to {TopicFilter}.",
            options.Host,
            options.Port,
            MqttTopics.CommandsTopicFilter);
    }

    public async Task PublishTelemetryAsync(TelemetryReading reading, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(reading);

        var message = factory.CreateApplicationMessageBuilder()
            .WithTopic(MqttTopics.Telemetry(reading.PlantId))
            .WithPayload(payload)
            .Build();

        await _client.PublishAsync(message, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_client.IsConnected)
        {
            await _client.DisconnectAsync(new MqttClientDisconnectOptions());
        }

        _client.Dispose();
    }
}
