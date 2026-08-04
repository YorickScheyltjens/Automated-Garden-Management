using GardenSystem.Application.Repositories;
using GardenSystem.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace GardenSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IGardenRepository, GardenRepository>();
        services.AddScoped<IPlantRepository, PlantRepository>();

        return services;
    }
}