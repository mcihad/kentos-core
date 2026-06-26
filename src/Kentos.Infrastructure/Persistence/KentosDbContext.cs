using Microsoft.EntityFrameworkCore;

namespace Kentos.Infrastructure.Persistence;

/// <summary>
/// Base for all module DbContexts. Applies an EF Core 10 named query filter
/// ("SoftDelete") to every <see cref="Kentos.SharedKernel.Entities.ISoftDeletable"/> entity.
/// </summary>
public abstract class KentosDbContext : DbContext
{
    /// <summary>Name of the soft-delete named query filter (can be disabled individually).</summary>
    public const string SoftDeleteFilter = "SoftDelete";

    protected KentosDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// Module contexts must call this AFTER applying their schema + configurations
    /// (so the filter loop sees every configured entity).
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplySoftDeleteFilters();
    }
}
