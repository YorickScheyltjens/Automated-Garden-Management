using System.Net;
using System.Net.Http.Json;
using GardenSystem.Api.Security;
using GardenSystem.Application.Gardens.Dtos;
using GardenSystem.Domain.Entities;
using GardenSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace GardenSystem.IntegrationTests;

public sealed class ApiKeyAuthenticationIntegrationTests : IAsyncLifetime
{
    private static readonly Guid OtherUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("gardensystem_apikey_tests")
        .WithUsername("gardensystem")
        .WithPassword("gardensystem")
        .Build();

    private WebApplicationFactory<Program>? _factory;
    private Guid _serviceUserId;
    private string _configuredApiKey = string.Empty;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        _factory = new TestApiFactory(_postgresContainer.GetConnectionString());

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GardenDbContext>();
        await dbContext.Database.MigrateAsync();

        var apiKeyOptions = scope.ServiceProvider.GetRequiredService<IOptions<ApiKeyOptions>>().Value;
        _serviceUserId = apiKeyOptions.ServiceUserId;
        _configuredApiKey = apiKeyOptions.Key;

        dbContext.Users.Add(new User
        {
            UserId = _serviceUserId,
            FirstName = "Service",
            LastName = "Account",
            Email = "service.account.integration@gardensystem.local",
            PasswordHash = "not-used-in-this-test",
            EmailVerified = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        dbContext.Users.Add(new User
        {
            UserId = OtherUserId,
            FirstName = "Other",
            LastName = "User",
            Email = "other.user.apikey.integration@gardensystem.local",
            PasswordHash = "not-used-in-this-test",
            EmailVerified = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        dbContext.Gardens.Add(new Garden
        {
            GardenId = Guid.NewGuid(),
            UserId = _serviceUserId,
            GardenName = "Service Account Garden",
            TotalSurfaceArea = 10m,
            LocationDescription = "Owned by the service account",
            TargetHumidityLevel = 55,
            CreatedAtUtc = DateTime.UtcNow
        });

        dbContext.Gardens.Add(new Garden
        {
            GardenId = Guid.NewGuid(),
            UserId = OtherUserId,
            GardenName = "Someone Else's Garden",
            TotalSurfaceArea = 10m,
            LocationDescription = "Not the service account's",
            TargetHumidityLevel = 55,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task GetGardens_WithValidApiKey_ReturnsOnlyServiceAccountGardens()
    {
        var factory = _factory ?? throw new InvalidOperationException("Test factory was not initialized.");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationHandler.HeaderName, _configuredApiKey);

        var response = await client.GetAsync("/api/v1/gardens");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var gardens = await response.Content.ReadFromJsonAsync<List<GardenResponseDto>>();
        Assert.NotNull(gardens);
        Assert.All(gardens!, garden => Assert.Equal("Service Account Garden", garden.GardenName));
    }

    [Fact]
    public async Task GetGardens_WithInvalidApiKey_ReturnsUnauthorized()
    {
        var factory = _factory ?? throw new InvalidOperationException("Test factory was not initialized.");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationHandler.HeaderName, "not-the-configured-key");

        var response = await client.GetAsync("/api/v1/gardens");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class TestApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<GardenDbContext>>();
                services.AddDbContext<GardenDbContext>(options => options.UseNpgsql(connectionString));
            });
        }
    }
}
