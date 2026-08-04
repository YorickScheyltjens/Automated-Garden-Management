using System.Net;
using System.Net.Http.Json;
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
            CreatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
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
