using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Gardens.Dtos;
using GardenSystem.Application.Plants.Dtos;
using GardenSystem.Domain.Entities;
using GardenSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace GardenSystem.IntegrationTests;

public sealed class PlantEndpointsIntegrationTests : IAsyncLifetime
{
    private static readonly Guid SeededUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("gardensystem_api_tests")
        .WithUsername("gardensystem")
        .WithPassword("gardensystem")
        .Build();

    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        _factory = new TestApiFactory(_postgresContainer.GetConnectionString());
        _client = _factory.CreateClient();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GardenDbContext>();

        await dbContext.Database.MigrateAsync();

        dbContext.Users.Add(new User
        {
            UserId = SeededUserId,
            FirstName = "Seeded",
            LastName = "User",
            Email = "seeded.user.integration@gardensystem.local",
            PasswordHash = "not-used-in-this-test",
            EmailVerified = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        dbContext.Users.Add(new User
        {
            UserId = OtherUserId,
            FirstName = "Other",
            LastName = "User",
            Email = "other.user.integration@gardensystem.local",
            PasswordHash = "not-used-in-this-test",
            EmailVerified = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        var jwtTokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var accessToken = jwtTokenGenerator.GenerateAccessToken(SeededUserId, "seeded.user.integration@gardensystem.local");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task DisposeAsync()
    {
        if (_client is not null)
        {
            _client.Dispose();
        }

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task PlantCrudFlow_CreateGetListDelete_WorksAsExpected()
    {
        var client = _client ?? throw new InvalidOperationException("Test client was not initialized.");

        var createGardenRequest = new CreateGardenRequestDto
        {
            GardenName = "Integration Garden",
            TotalSurfaceArea = 20m,
            LocationDescription = "Integration Location",
            Latitude = 51.2194m,
            Longitude = 4.4025m,
            TargetHumidityLevel = 60
        };

        var createGardenResponse = await client.PostAsJsonAsync("/api/v1/gardens", createGardenRequest);
        Assert.Equal(HttpStatusCode.Created, createGardenResponse.StatusCode);

        var createdGarden = await createGardenResponse.Content.ReadFromJsonAsync<GardenResponseDto>();
        Assert.NotNull(createdGarden);

        var createPlantRequest = new CreatePlantRequestDto
        {
            GardenId = createdGarden!.GardenId,
            PlantName = "Tomato",
            Species = "Solanum lycopersicum",
            PlantType = GardenSystem.Domain.Enums.PlantType.Vegetable,
            PlantationDate = new DateOnly(2026, 8, 4),
            SurfaceAreaRequired = 1.2m,
            IdealHumidityLevel = 58
        };

        var createPlantResponse = await client.PostAsJsonAsync($"/api/v1/gardens/{createdGarden.GardenId}/plants", createPlantRequest);
        Assert.Equal(HttpStatusCode.Created, createPlantResponse.StatusCode);

        var createdPlant = await createPlantResponse.Content.ReadFromJsonAsync<PlantResponseDto>();
        Assert.NotNull(createdPlant);

        var getPlantResponse = await client.GetAsync($"/api/v1/plants/{createdPlant!.PlantId}");
        Assert.Equal(HttpStatusCode.OK, getPlantResponse.StatusCode);

        var fetchedPlant = await getPlantResponse.Content.ReadFromJsonAsync<PlantResponseDto>();
        Assert.NotNull(fetchedPlant);
        Assert.Equal(createdPlant.PlantId, fetchedPlant!.PlantId);

        var listPlantsResponse = await client.GetAsync($"/api/v1/gardens/{createdGarden.GardenId}/plants");
        Assert.Equal(HttpStatusCode.OK, listPlantsResponse.StatusCode);

        var plantsInGarden = await listPlantsResponse.Content.ReadFromJsonAsync<List<PlantResponseDto>>();
        Assert.NotNull(plantsInGarden);
        Assert.Contains(plantsInGarden!, plant => plant.PlantId == createdPlant.PlantId);

        var deletePlantResponse = await client.DeleteAsync($"/api/v1/plants/{createdPlant.PlantId}");
        Assert.Equal(HttpStatusCode.NoContent, deletePlantResponse.StatusCode);

        var getDeletedPlantResponse = await client.GetAsync($"/api/v1/plants/{createdPlant.PlantId}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedPlantResponse.StatusCode);

        var listAfterDeleteResponse = await client.GetAsync($"/api/v1/gardens/{createdGarden.GardenId}/plants");
        Assert.Equal(HttpStatusCode.OK, listAfterDeleteResponse.StatusCode);

        var plantsAfterDelete = await listAfterDeleteResponse.Content.ReadFromJsonAsync<List<PlantResponseDto>>();
        Assert.NotNull(plantsAfterDelete);
        Assert.DoesNotContain(plantsAfterDelete!, plant => plant.PlantId == createdPlant.PlantId);
    }

    [Fact]
    public async Task CreatePlant_WhenGardenIsFull_ReturnsConflictWithNumericDetails()
    {
        var client = _client ?? throw new InvalidOperationException("Test client was not initialized.");

        var createGardenRequest = new CreateGardenRequestDto
        {
            GardenName = "Capacity Garden",
            TotalSurfaceArea = 10m,
            LocationDescription = "Integration Location",
            Latitude = 51.2194m,
            Longitude = 4.4025m,
            TargetHumidityLevel = 60
        };

        var createGardenResponse = await client.PostAsJsonAsync("/api/v1/gardens", createGardenRequest);
        Assert.Equal(HttpStatusCode.Created, createGardenResponse.StatusCode);

        var createdGarden = await createGardenResponse.Content.ReadFromJsonAsync<GardenResponseDto>();
        Assert.NotNull(createdGarden);

        var firstPlantResponse = await client.PostAsJsonAsync(
            $"/api/v1/gardens/{createdGarden!.GardenId}/plants",
            new CreatePlantRequestDto
            {
                GardenId = createdGarden.GardenId,
                PlantName = "Large Plant",
                Species = "Species A",
                PlantType = GardenSystem.Domain.Enums.PlantType.Vegetable,
                PlantationDate = new DateOnly(2026, 8, 4),
                SurfaceAreaRequired = 9.7m,
                IdealHumidityLevel = 58
            });

        Assert.Equal(HttpStatusCode.Created, firstPlantResponse.StatusCode);

        var overflowPlantResponse = await client.PostAsJsonAsync(
            $"/api/v1/gardens/{createdGarden.GardenId}/plants",
            new CreatePlantRequestDto
            {
                GardenId = createdGarden.GardenId,
                PlantName = "Overflow Plant",
                Species = "Species B",
                PlantType = GardenSystem.Domain.Enums.PlantType.Flower,
                PlantationDate = new DateOnly(2026, 8, 5),
                SurfaceAreaRequired = 0.5m,
                IdealHumidityLevel = 57
            });

        Assert.Equal(HttpStatusCode.Conflict, overflowPlantResponse.StatusCode);

        var problem = await overflowPlantResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Conflict, problem!.Status);
        Assert.Contains("requires 0.5m2", problem.Detail);
        Assert.Contains("only 0.3m2", problem.Detail);
        Assert.Contains("10m2 garden", problem.Detail);
    }

    [Fact]
    public async Task GetGardens_WithoutBearerToken_ReturnsUnauthorized()
    {
        var factory = _factory ?? throw new InvalidOperationException("Test factory was not initialized.");
        using var anonymousClient = factory.CreateClient();

        var response = await anonymousClient.GetAsync("/api/v1/gardens");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetGarden_OwnedByAnotherUser_ReturnsNotFound()
    {
        var client = _client ?? throw new InvalidOperationException("Test client was not initialized.");
        var factory = _factory ?? throw new InvalidOperationException("Test factory was not initialized.");

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GardenDbContext>();

        var otherUsersGarden = new Garden
        {
            GardenId = Guid.NewGuid(),
            UserId = OtherUserId,
            GardenName = "Someone Else's Garden",
            TotalSurfaceArea = 10m,
            LocationDescription = "Not yours",
            TargetHumidityLevel = 50,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Gardens.Add(otherUsersGarden);
        await dbContext.SaveChangesAsync();

        var getResponse = await client.GetAsync($"/api/v1/gardens/{otherUsersGarden.GardenId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        var listResponse = await client.GetAsync("/api/v1/gardens");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var gardens = await listResponse.Content.ReadFromJsonAsync<List<GardenResponseDto>>();
        Assert.NotNull(gardens);
        Assert.DoesNotContain(gardens!, garden => garden.GardenId == otherUsersGarden.GardenId);
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
