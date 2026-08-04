using GardenSystem.Infrastructure;
using GardenSystem.Infrastructure.Persistence;
using GardenSystem.TelemetryConsumer;
using GardenSystem.TelemetryConsumer.Configuration;
using GardenSystem.TelemetryConsumer.Messaging;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<GardenDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddInfrastructure();

builder.Services.AddOptions<RabbitMqOptions>().Bind(builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddSingleton<ITelemetryMessagingClient, RabbitMqTelemetryMessagingClient>();

builder.Services.AddHostedService<TelemetryConsumerService>();

var host = builder.Build();
host.Run();
