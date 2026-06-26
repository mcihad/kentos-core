using Kentos.SharedKernel.Entities;
using Kentos.SharedKernel.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kentos.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Applies audit fields, UUIDv7 generation, version increment and hard-delete to
/// soft-delete conversion for <see cref="BaseEntity"/> instances.
/// </summary>
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;

    public AuditableEntityInterceptor(ICurrentUser currentUser, TimeProvider clock)
    {
        _currentUser = currentUser;
        _clock = clock;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = _clock.GetUtcNow();
        var actor = _currentUser.UserName ?? _currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.Uuid == Guid.Empty)
                    {
                        entry.Entity.Uuid = Guid.CreateVersion7();
                    }

                    entry.Entity.CreatedBy = actor;
                    entry.Entity.CreatedAt = now;
                    entry.Entity.Version = 1;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedBy = actor;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.Version += 1;
                    break;

                case EntityState.Deleted:
                    // Convert hard delete to soft delete.
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedBy = actor;
                    entry.Entity.DeletedAt = now;
                    break;
            }
        }
    }
}
