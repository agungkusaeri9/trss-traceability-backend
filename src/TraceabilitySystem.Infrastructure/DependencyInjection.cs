using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Infrastructure.Persistence;
using TraceabilitySystem.Infrastructure.Persistence.Repositories;
using TraceabilitySystem.Infrastructure.Services;

namespace TraceabilitySystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
        {
            if (configuration["UseInMemoryDatabase"] != "true")
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                options.UseMySql(connectionString, new MySqlServerVersion(new System.Version(8, 0, 21)));
            }
        });

        // JWT Settings
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPartRepository, PartRepository>();
        services.AddScoped<IProcessRepository, ProcessRepository>();
        services.AddScoped<IParameterRepository, ParameterRepository>();

        // Infrastructure Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
