using System.Text;
using System.Text.Json;
using GardenSystem.TelemetryConsumer.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GardenSystem.TelemetryConsumer.Messaging;

public sealed class RabbitMqTelemetryMessagingClient(
    IOptions<RabbitMqOptions> rabbitMqOptions,
    ILogger<RabbitMqTelemetryMessagingClient> logger) : ITelemetryMessagingClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private IConnection? _connection;
    private IChannel? _channel;

    public async Task ConnectAsync(Func<TelemetryMessage, Task> onTelemetryReceived, CancellationToken cancellationToken)
    {
        var options = rabbitMqOptions.Value;
        var factory = new ConnectionFactory
        {
            HostName = options.Host,
            Port = options.Port,
            UserName = options.Username,
            Password = options.Password
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: AmqpTopics.TelemetryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: AmqpTopics.TelemetryQueueName,
            exchange: AmqpTopics.TopicExchange,
            routingKey: AmqpTopics.TelemetryBindingPattern,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, deliverEventArgs) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(deliverEventArgs.Body.ToArray());
                var message = JsonSerializer.Deserialize<TelemetryMessage>(json, JsonOptions);

                if (message is not null)
                {
                    await onTelemetryReceived(message);
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to process a telemetry message.");
            }
            finally
            {
                await _channel.BasicAckAsync(deliverEventArgs.DeliveryTag, false, cancellationToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: AmqpTopics.TelemetryQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Connected to RabbitMQ at {Host}:{Port} and consuming from queue {QueueName}.",
            options.Host,
            options.Port,
            AmqpTopics.TelemetryQueueName);
    }

    public async Task PublishIrrigationCommandAsync(Guid plantId, CancellationToken cancellationToken)
    {
        if (_channel is null)
        {
            throw new InvalidOperationException("Not connected to RabbitMQ.");
        }

        await _channel.BasicPublishAsync(
            exchange: AmqpTopics.TopicExchange,
            routingKey: AmqpTopics.IrrigationCommandRoutingKey(plantId),
            body: ReadOnlyMemory<byte>.Empty,
            cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
