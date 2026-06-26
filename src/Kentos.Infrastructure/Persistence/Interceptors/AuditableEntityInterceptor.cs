using Kentos.SharedKernel.Entities;
using Kentos.SharedKernel.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kentos.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Applies audit fields (<see cref="IAuditable"/>), hard-delete to soft-delete
/// conversion (<see cref="ISoftDeletable"/>), plus UUIDv7 generation and version
/// increment for <see cref="BaseEntity"/> instances. Interface-based so it also
/// covers entities that cannot inherit <see cref="BaseEntity"/> (e.g. ASP.NET
/// Identity users/roles), which obtain their UUID via a DB default instead.
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

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not (IAuditable or ISoftDeletable))
            {
                continue;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    // BaseEntity carries Uuid/Version; Identity entities get their UUID from a DB default.
                    if (entry.Entity is BaseEntity addedEntity)
                    {
                        if (addedEntity.Uuid == Guid.Empty)
                        {
                            addedEntity.Uuid = Guid.CreateVersion7();
                        }

                        addedEntity.Version = 1;
                    }

                    if (entry.Entity is IAuditable addedAuditable)
                    {
                        addedAuditable.CreatedBy = actor;
                        addedAuditable.CreatedAt = now;
                    }

                    break;

                case EntityState.Modified:
                    if (entry.Entity is BaseEntity modifiedEntity)
                    {
                        modifiedEntity.Version += 1;
                    }

                    if (entry.Entity is IAuditable modifiedAuditable)
                    {
                        modifiedAuditable.UpdatedBy = actor;
                        modifiedAuditable.UpdatedAt = now;
                    }

                    break;

                case EntityState.Deleted when entry.Entity is ISoftDeletable deletable:
                    // Convert hard delete to soft delete.
                    entry.State = EntityState.Modified;
                    deletable.IsDeleted = true;
                    deletable.DeletedBy = actor;
                    deletable.DeletedAt = now;
                    break;
            }
        }
    }
}
