using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Infrastructure.Persistence;
using TraceabilitySystem.Infrastructure.Persistence.Repositories;
using TraceabilitySystem.Infrastructure.Services;

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
        services.AddScoped<IStockInRepository, StockInRepository>();
        services.AddScoped<IIssueRepository, IssueRepository>();
        services.AddScoped<IPrinterRepository, PrinterRepository>();
        services.AddScoped<IAppConfigRepository, AppConfigRepository>();
        services.AddScoped<IProcessLogRepository, ProcessLogRepository>();
        services.AddScoped<IMqttPrintRequestRepository, MqttPrintRequestRepository>();
        services.AddScoped<ISerialNumberRepository, SerialNumberRepository>();
        services.AddScoped<ISerialNumberRepository, SerialNumberRepository>();
        services.AddScoped<IIssueTransactionRepository, IssueTransactionRepository>();
        services.AddScoped<IStockInReworkRepository, StockInReworkRepository>();

        // Infrastructure Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IPrintService, PrintService>();

        return services;
    }


}
