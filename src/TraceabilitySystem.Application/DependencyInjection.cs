using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Application.Mappings;
using TraceabilitySystem.Application.Services;

namespace TraceabilitySystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<UserMappingProfile>();
        });
        services.AddValidatorsFromAssemblyContaining<UserMappingProfile>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
