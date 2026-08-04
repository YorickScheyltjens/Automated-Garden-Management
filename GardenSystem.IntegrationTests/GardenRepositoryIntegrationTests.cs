using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Entities;
using GardenSystem.Infrastructure.Persistence;
using GardenSystem.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace GardenSystem.IntegrationTests;

public sealed class GardenRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("gardensystem")
        .WithUsername("gardensystem")
        .WithPassword("gardensystem")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task ListByUserIdAsync_Excludes_SoftDeleted_Garden()
    {
        var userId = Guid.NewGuid();
        var gardenId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await using (var setupContext = CreateDbContext())
        {
            setupContext.Users.Add(new User
            {
                UserId = userId,
                FirstName = "Integration",
                LastName = "User",
                Email = "integration.user@example.com",
                CreatedAtUtc = now
            });

            await setupContext.SaveChangesAsync();

            IGardenRepository gardenRepository = new GardenRepository(setupContext);

            await gardenRepository.AddAsync(new Garden
            {
                GardenId = gardenId,
                UserId = userId,
                GardenName = "Integration Garden",
                TotalSurfaceArea = 15.0m,
                LocationDescription = "Integration Test Plot",
                Latitude = 51.2194m,
                Longitude = 4.4025m,
                TargetHumidityLevel = 60,
                CreatedAtUtc = now,
                DeletedAtUtc = null
            });
        }

        await using (var listContext = CreateDbContext())
        {
            IGardenRepository gardenRepository = new GardenRepository(listContext);

            var gardens = await gardenRepository.ListByUserIdAsync(userId);

            Assert.Single(gardens);
            Assert.Equal(gardenId, gardens[0].GardenId);
        }

        await using (var deleteContext = CreateDbContext())
        {
            IGardenRepository gardenRepository = new GardenRepository(deleteContext);
            await gardenRepository.SoftDeleteAsync(gardenId);
        }

        await using (var verifyContext = CreateDbContext())
        {
            IGardenRepository gardenRepository = new GardenRepository(verifyContext);

            var gardens = await gardenRepository.ListByUserIdAsync(userId);

            Assert.Empty(gardens);
        }
    }

    private GardenDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GardenDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        return new GardenDbContext(options);
    }
}