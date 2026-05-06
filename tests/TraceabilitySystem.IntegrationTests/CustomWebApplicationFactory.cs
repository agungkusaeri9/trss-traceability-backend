using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TraceabilitySystem.Infrastructure.Persistence;

namespace TraceabilitySystem.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "UseInMemoryDatabase", "true" },
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=dummy;User=root;Password=dummy;" },
                { "JwtSettings:Secret", "SuperSecretKeyForTestingSuperSecretKeyForTesting" },
                { "JwtSettings:Issuer", "TraceabilitySystem" },
                { "JwtSettings:Audience", "TraceabilitySystem" }
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });
        });
    }
}
