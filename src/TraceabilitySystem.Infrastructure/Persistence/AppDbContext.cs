using Microsoft.EntityFrameworkCore;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Part> Parts => Set<Part>();

    public static bool IsInMemory { get; private set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        IsInMemory = Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        if (Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                // Table names to lowercase snake_case
                var tableName = entity.GetTableName();
                if (tableName != null) entity.SetTableName(ToSnakeCase(tableName));

                // Column names to lowercase snake_case
                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnName(ToSnakeCase(property.Name));
                }

                // Keys and Indexes
                foreach (var key in entity.GetKeys())
                {
                    var keyName = key.GetName();
                    if (keyName != null) key.SetName(ToSnakeCase(keyName));
                }

                foreach (var index in entity.GetIndexes())
                {
                    var indexName = index.GetDatabaseName();
                    if (indexName != null) index.SetDatabaseName(ToSnakeCase(indexName));
                }

                foreach (var foreignKey in entity.GetForeignKeys())
                {
                    var constraintName = foreignKey.GetConstraintName();
                    if (constraintName != null) foreignKey.SetConstraintName(ToSnakeCase(constraintName));
                }
            }
        }
    }

    private string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var startUnderscore = input.StartsWith("_");
        var res = System.Text.RegularExpressions.Regex.Replace(input, @"([a-z0-9])([A-Z])", "$1_$2").ToLower();
        return startUnderscore ? "_" + res : res;
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is User user)
            {
                if (entry.State == EntityState.Added) user.CreatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Modified) user.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is RefreshToken token)
            {
                if (entry.State == EntityState.Added) token.CreatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Modified) token.UpdatedAt = DateTime.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
