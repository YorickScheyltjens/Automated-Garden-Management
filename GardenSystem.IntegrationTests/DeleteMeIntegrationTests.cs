using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Auth.Dtos;
using GardenSystem.Application.Gardens.Dtos;
using GardenSystem.Application.Plants.Dtos;
using GardenSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace GardenSystem.IntegrationTests;

public sealed class DeleteMeIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("gardensystem_delete_me_tests")
        .WithUsername("gardensystem")
        .WithPassword("gardensystem")
        .Build();

    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private CapturingEmailSender? _emailSender;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        _factory = new TestApiFactory(_postgresContainer.GetConnectionString());
        _client = _factory.CreateClient();
        _emailSender = (CapturingEmailSender)_factory.Services.GetRequiredService<IEmailSender>();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GardenDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task DeleteMe_SoftDeletesUserAndCascadesToGardensAndPlants()
    {
        var client = _client ?? throw new InvalidOperationException("Test client was not initialized.");
        var emailSender = _emailSender ?? throw new InvalidOperationException("Email sender was not initialized.");
        var factory = _factory ?? throw new InvalidOperationException("Test factory was not initialized.");

        const string email = "delete.me.integration@gardensystem.local";
        const string password = "supersecret1";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterUserRequestDto { FirstName = "Delete", LastName = "Me", Email = email, Password = password });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registeredUser = await registerResponse.Content.ReadFromJsonAsync<UserResponseDto>();

        var code = Regex.Match(emailSender.LastBody ?? string.Empty, @"\b\d{6}\b").Value;
        await client.PostAsJsonAsync("/api/v1/auth/verify-email", new VerifyEmailRequestDto { Email = email, Code = code });

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequestDto { Email = email, Password = password });
        var tokens = await loginResponse.Content.ReadFromJsonAsync<AuthTokensResponseDto>();
        Assert.NotNull(tokens);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var createGardenResponse = await client.PostAsJsonAsync(
            "/api/v1/gardens",
            new CreateGardenRequestDto
            {
                GardenName = "Garden To Delete",
                TotalSurfaceArea = 10m,
                LocationDescription = "Backyard",
                TargetHumidityLevel = 55
            });
        Assert.Equal(HttpStatusCode.Created, createGardenResponse.StatusCode);
        var garden = await createGardenResponse.Content.ReadFromJsonAsync<GardenResponseDto>();
        Assert.NotNull(garden);

        var createPlantResponse = await client.PostAsJsonAsync(
            $"/api/v1/gardens/{garden!.GardenId}/plants",
            new CreatePlantRequestDto
            {
                GardenId = garden.GardenId,
                PlantName = "Tomato",
                Species = "Solanum lycopersicum",
                PlantType = GardenSystem.Domain.Enums.PlantType.Vegetable,
                PlantationDate = new DateOnly(2026, 8, 5),
                SurfaceAreaRequired = 1m,
                IdealHumidityLevel = 55
            });
        Assert.Equal(HttpStatusCode.Created, createPlantResponse.StatusCode);
        var plant = await createPlantResponse.Content.ReadFromJsonAsync<PlantResponseDto>();
        Assert.NotNull(plant);

        var deleteMeResponse = await client.DeleteAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.NoContent, deleteMeResponse.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GardenDbContext>();

            var storedUser = await dbContext.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.UserId == registeredUser!.UserId);
            Assert.NotNull(storedUser);
            Assert.NotNull(storedUser!.DeletedAtUtc);

            var storedGarden = await dbContext.Gardens.IgnoreQueryFilters()
                .FirstOrDefaultAsync(g => g.GardenId == garden.GardenId);
            Assert.NotNull(storedGarden);
            Assert.NotNull(storedGarden!.DeletedAtUtc);

            var storedPlant = await dbContext.Plants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.PlantId == plant!.PlantId);
            Assert.NotNull(storedPlant);
            Assert.NotNull(storedPlant!.DeletedAtUtc);
        }

        var gardensAfterDelete = await client.GetAsync("/api/v1/gardens");
        Assert.Equal(HttpStatusCode.OK, gardensAfterDelete.StatusCode);
        var remainingGardens = await gardensAfterDelete.Content.ReadFromJsonAsync<List<GardenResponseDto>>();
        Assert.NotNull(remainingGardens);
        Assert.Empty(remainingGardens!);

        var loginAfterDeleteResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequestDto { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.Unauthorized, loginAfterDeleteResponse.StatusCode);
    }

    private sealed class TestApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<GardenDbContext>>();
                services.AddDbContext<GardenDbContext>(options => options.UseNpgsql(connectionString));

                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender, CapturingEmailSender>();
            });
        }
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public string? LastBody { get; private set; }

        public Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
        {
            LastBody = body;
            return Task.CompletedTask;
        }
    }
}
