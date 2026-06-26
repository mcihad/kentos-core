using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kentos.Modules.Settlement.Infrastructure;

/// <summary>Design-time factory so `dotnet ef` can build the context without the host.</summary>
public sealed class SettlementDbContextDesignFactory : IDesignTimeDbContextFactory<SettlementDbContext>
{
    public SettlementDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=kentos;Username=kentos;Password=kentos";

        var options = new DbContextOptionsBuilder<SettlementDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new SettlementDbContext(options);
    }
}
