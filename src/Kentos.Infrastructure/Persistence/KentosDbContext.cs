using System.Reflection;
using Kentos.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kentos.Infrastructure.Persistence;

/// <summary>
/// Base for all module DbContexts. Applies an EF Core 10 named query filter
/// ("SoftDelete") to every <see cref="ISoftDeletable"/> entity.
/// </summary>
public abstract class KentosDbContext : DbContext
{
    /// <summary>Name of the soft-delete named query filter (can be disabled individually).</summary>
    public const string SoftDeleteFilter = "SoftDelete";

    private static readonly MethodInfo SetSoftDeleteFilterMethod =
        typeof(KentosDbContext).GetMethod(nameof(SetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!;

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

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.BaseType is null && typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                SetSoftDeleteFilterMethod.MakeGenericMethod(entityType.ClrType).Invoke(null, [modelBuilder]);
            }
        }
    }

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ISoftDeletable
        => modelBuilder.Entity<TEntity>().HasQueryFilter(SoftDeleteFilter, e => !e.IsDeleted);
}
