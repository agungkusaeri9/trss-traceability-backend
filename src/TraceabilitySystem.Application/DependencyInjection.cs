using FluentValidation;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Application.Services;

namespace TraceabilitySystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TypeAdapterConfig.GlobalSettings);

        services.AddValidatorsFromAssemblyContaining<UserService>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPartService, PartService>();
        services.AddScoped<IProcessService, ProcessService>();
        services.AddScoped<IParameterService, ParameterService>();

        return services;
    }
}