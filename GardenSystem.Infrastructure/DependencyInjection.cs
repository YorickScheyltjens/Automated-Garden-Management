using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Repositories;
using GardenSystem.Infrastructure.Email;
using GardenSystem.Infrastructure.Persistence.Repositories;
using GardenSystem.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace GardenSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IGardenRepository, GardenRepository>();
        services.AddScoped<IPlantRepository, PlantRepository>();
        services.AddScoped<IPlantStateRepository, PlantStateRepository>();
        services.AddScoped<IIrrigationEventRepository, IrrigationEventRepository>();
        services.AddScoped<IReportingRepository, ReportingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}