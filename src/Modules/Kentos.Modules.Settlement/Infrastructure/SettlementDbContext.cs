using Kentos.Infrastructure.Persistence;
using Kentos.Modules.Settlement.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kentos.Modules.Settlement.Infrastructure;

/// <summary>The Settlement module DbContext (Turkish 'yerlesim' schema).</summary>
public sealed class SettlementDbContext : KentosDbContext
{
    public const string Schema = "yerlesim";

    public SettlementDbContext(DbContextOptions<SettlementDbContext> options) : base(options)
    {
    }

    public DbSet<Province> Provinces => Set<Province>();

    public DbSet<District> Districts => Set<District>();

    public DbSet<Neighborhood> Neighborhoods => Set<Neighborhood>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SettlementDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
