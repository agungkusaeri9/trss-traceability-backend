using FluentValidation;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Application.Services;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Application.DTOs.Process;

namespace TraceabilitySystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TypeAdapterConfig.GlobalSettings);

        TypeAdapterConfig<Process, ProcessDto>.NewConfig()
            .Map(dest => dest.Parameters, src => src.ProcessParameters != null
                ? src.ProcessParameters.Select(pp => pp.Parameter)
                : null);

        services.AddValidatorsFromAssemblyContaining<UserService>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPartService, PartService>();
        services.AddScoped<IProcessService, ProcessService>();
        services.AddScoped<IParameterService, ParameterService>();
        services.AddScoped<IStockInService, StockInService>();
        services.AddScoped<IPrinterService, PrinterService>();
        services.AddScoped<IAppConfigService, AppConfigService>();
        services.AddScoped<IProcessLogService, ProcessLogService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}