using GardenSystem.SensorSimulator;
using GardenSystem.SensorSimulator.Configuration;
using GardenSystem.SensorSimulator.Mqtt;
using GardenSystem.SensorSimulator.PlantRoster;
using MQTTnet;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<ApiOptions>().Bind(builder.Configuration.GetSection("Api"));
builder.Services.AddOptions<MqttOptions>().Bind(builder.Configuration.GetSection("Mqtt"));
builder.Services.AddOptions<SimulationOptions>().Bind(builder.Configuration.GetSection("Simulation"));

builder.Services.AddHttpClient<IPlantRosterClient, ApiPlantRosterClient>(httpClient =>
{
    var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:8080";
    httpClient.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddSingleton(new MqttClientFactory());
builder.Services.AddSingleton<IIrrigationMqttClient, IrrigationMqttClient>();

builder.Services.AddHostedService<SensorSimulatorWorker>();

var host = builder.Build();
host.Run();
