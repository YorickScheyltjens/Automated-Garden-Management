using System.Text;
using System.Text.Json;
using GardenSystem.Domain.Entities;
using GardenSystem.Domain.Enums;
using GardenSystem.Infrastructure;
using GardenSystem.Infrastructure.Persistence;
using GardenSystem.TelemetryConsumer;
using GardenSystem.TelemetryConsumer.Configuration;
using GardenSystem.TelemetryConsumer.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace GardenSystem.IntegrationTests;

public sealed class TelemetryConsumerIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("gardensystem_telemetry_tests")
        .WithUsername("gardensystem")
        .WithPassword("gardensystem")
        .Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:3-management-alpine")
        .WithUsername("gardensystem")
        .WithPassword("gardensystem")
        .Build();

    private IHost? _host;
    private IConnection? _testConnection;
    private IChannel? _testChannel;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgresContainer.StartAsync(), _rabbitMqContainer.StartAsync());

        var hostBuilder = Host.CreateApplicationBuilder();

        hostBuilder.Services.AddDbContext<GardenDbContext>(options => options.UseNpgsql(_postgresContainer.GetConnectionString()));
        hostBuilder.Services.AddInfrastructure();

        hostBuilder.Services.AddOptions<RabbitMqOptions>().Configure(options =>
        {
            options.Host = _rabbitMqContainer.Hostname;
            options.Port = _rabbitMqContainer.GetMappedPublicPort(5672);
            options.Username = "gardensystem";
            options.Password = "gardensystem";
        });

        hostBuilder.Services.AddSingleton<ITelemetryMessagingClient, RabbitMqTelemetryMessagingClient>();
        hostBuilder.Services.AddHostedService<TelemetryConsumerService>();

        _host = hostBuilder.Build();

        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GardenDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        await _host.StartAsync();

        var connectionFactory = new ConnectionFactory
        {
            HostName = _rabbitMqContainer.Hostname,
            Port = _rabbitMqContainer.GetMappedPublicPort(5672),
            UserName = "gardensystem",
            Password = "gardensystem"
        };

        _testConnection = await connectionFactory.CreateConnectionAsync();
        _testChannel = await _testConnection.CreateChannelAsync();
    }

    public async Task DisposeAsync()
    {
        if (_testChannel is not null)
        {
            await _testChannel.DisposeAsync();
        }

        if (_testConnection is not null)
        {
            await _testConnection.DisposeAsync();
        }

        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        await _rabbitMqContainer.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task TelemetryBelowIdealHumidity_PublishesIrrigationCommand_AndUpdatesPlantState()
    {
        var host = _host ?? throw new InvalidOperationException("Host was not initialized.");
        var testChannel = _testChannel ?? throw new InvalidOperationException("Test channel was not initialized.");

        var userId = Guid.NewGuid();
        var gardenId = Guid.NewGuid();
        var plantId = Guid.NewGuid();

        await using (var scope = host.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GardenDbContext>();

            dbContext.Users.Add(new User
            {
                UserId = userId,
                FirstName = "Integration",
                LastName = "User",
                Email = "telemetry.integration@example.com",
                CreatedAtUtc = DateTime.UtcNow
            });

            dbContext.Gardens.Add(new Garden
            {
                GardenId = gardenId,
                UserId = userId,
                GardenName = "Telemetry Test Garden",
                TotalSurfaceArea = 10m,
                LocationDescription = "Integration Test Plot",
                TargetHumidityLevel = 60,
                CreatedAtUtc = DateTime.UtcNow
            });

            dbContext.Plants.Add(new Plant
            {
                PlantId = plantId,
                GardenId = gardenId,
                PlantName = "Test Plant",
                Species = "Test Species",
                PlantType = PlantType.Vegetable,
                PlantationDate = DateOnly.FromDateTime(DateTime.UtcNow),
                SurfaceAreaRequired = 1m,
                IdealHumidityLevel = 55,
                CreatedAtUtc = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        var commandsQueue = await testChannel.QueueDeclareAsync(queue: string.Empty, durable: false, exclusive: true, autoDelete: true);
        await testChannel.QueueBindAsync(commandsQueue.QueueName, "amq.topic", "irrigation.*.commands");

        var telemetryPayload = JsonSerializer.Serialize(new
        {
            PlantId = plantId,
            CurrentHumidityLevel = 40m,
            IsCurrentlyIrrigating = false,
            TimestampUtc = DateTime.UtcNow
        });

        await testChannel.BasicPublishAsync(
            exchange: "amq.topic",
            routingKey: $"sensors.{plantId}.telemetry",
            body: Encoding.UTF8.GetBytes(telemetryPayload));

        BasicGetResult? commandResult = null;
        for (var attempt = 0; attempt < 40 && commandResult is null; attempt++)
        {
            commandResult = await testChannel.BasicGetAsync(commandsQueue.QueueName, autoAck: true);

            if (commandResult is null)
            {
                await Task.Delay(500);
            }
        }

        Assert.NotNull(commandResult);
        Assert.Equal($"irrigation.{plantId}.commands", commandResult!.RoutingKey);

        PlantState? plantState = null;
        for (var attempt = 0; attempt < 40 && plantState is null; attempt++)
        {
            await using var scope = host.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GardenDbContext>();
            plantState = await dbContext.PlantStates.FirstOrDefaultAsync(p => p.PlantId == plantId);

            if (plantState is null)
            {
                await Task.Delay(500);
            }
        }

        Assert.NotNull(plantState);
        Assert.True(plantState!.IsCurrentlyIrrigating);
        Assert.Equal(40m, plantState.CurrentHumidityLevel);
    }
}
