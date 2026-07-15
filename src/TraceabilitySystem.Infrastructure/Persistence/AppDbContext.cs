using Microsoft.EntityFrameworkCore;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<Process> Processes => Set<Process>();
    public DbSet<Parameter> Parameters => Set<Parameter>();
    public DbSet<ProcessParameter> ProcessParameters => Set<ProcessParameter>();
    public DbSet<StockIn> StockIns => Set<StockIn>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<Printer> Printers => Set<Printer>();
    public DbSet<AppConfig> AppConfigs => Set<AppConfig>();
    public DbSet<ProcessLog> ProcessLogs => Set<ProcessLog>();
    public DbSet<ProcessLogDetail> ProcessLogDetails => Set<ProcessLogDetail>();
    public DbSet<MqttPrintRequest> MqttPrintRequests => Set<MqttPrintRequest>();
    public DbSet<SerialNumber> SerialNumbers => Set<SerialNumber>();

    public DbSet<SerialNumberIssue> SerialNumberIssues => Set<SerialNumberIssue>();

    public DbSet<SerialNumberRelation> SerialNumberRelations => Set<SerialNumberRelation>();
    public DbSet<IssueTransaction> IssueTransactions => Set<IssueTransaction>();
    public DbSet<StockInRework> StockInReworks => Set<StockInRework>();
    public DbSet<PrintHistory> PrintHistories => Set<PrintHistory>();

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
            else if (entry.Entity is StockIn stockIn)
            {
                if (entry.State == EntityState.Added) stockIn.CreatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Modified) stockIn.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Issue issue)
            {
                if (entry.State == EntityState.Added) issue.CreatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Modified) issue.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Printer printer)
            {
                if (entry.State == EntityState.Added) printer.CreatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Modified) printer.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is AppConfig config)
            {
                if (entry.State == EntityState.Added) config.CreatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Modified) config.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is ProcessLog log)
            {
                if (entry.State == EntityState.Added) log.CreatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Modified) log.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is ProcessLogDetail detail)
            {
                if (entry.State == EntityState.Added) detail.CreatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Modified) detail.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is MqttPrintRequest mqttRequest)
            {
                if (entry.State == EntityState.Added) mqttRequest.CreatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Modified) mqttRequest.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is SerialNumber serialNumber)
            {
                if (entry.State == EntityState.Added) serialNumber.CreatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Modified) serialNumber.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is IssueTransaction issueTransaction)
            {
                if (entry.State == EntityState.Added) issueTransaction.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is SerialNumberIssue serialNumberIssue)
            {
                if (entry.State == EntityState.Added) serialNumberIssue.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is StockInRework stockInRework)
            {
                if (entry.State == EntityState.Added) stockInRework.CreatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Modified) stockInRework.UpdatedAt = DateTime.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
