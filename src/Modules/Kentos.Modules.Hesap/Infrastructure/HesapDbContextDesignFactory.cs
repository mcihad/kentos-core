using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kentos.Modules.Hesap.Infrastructure;

/// <summary>Design-time factory so `dotnet ef` can build the context without the host.</summary>
public sealed class HesapDbContextDesignFactory : IDesignTimeDbContextFactory<HesapDbContext>
{
    public HesapDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=kentos;Username=kentos;Password=kentos";

        var options = new DbContextOptionsBuilder<HesapDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new HesapDbContext(options);
    }
}
