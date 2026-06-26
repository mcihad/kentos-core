using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kentos.Infrastructure.Persistence;

/// <summary>Design-time factory so `dotnet ef` can build the auditing context without the host.</summary>
public sealed class AuditingDbContextDesignFactory : IDesignTimeDbContextFactory<AuditingDbContext>
{
    public AuditingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=kentos;Username=kentos;Password=kentos";

        var options = new DbContextOptionsBuilder<AuditingDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AuditingDbContext(options);
    }
}
