using GardenSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GardenSystem.Infrastructure.Persistence;

public sealed class GardenDbContext(DbContextOptions<GardenDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Garden> Gardens => Set<Garden>();
    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<PlantState> PlantStates => Set<PlantState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GardenDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}