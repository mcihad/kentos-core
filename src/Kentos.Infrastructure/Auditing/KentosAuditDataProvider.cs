using Audit.Core;
using Audit.EntityFramework;
using Kentos.SharedKernel.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Kentos.Infrastructure.Auditing;

/// <summary>
/// Audit.NET data provider: maps EF Core audit events to <see cref="AuditLog"/> and
/// persists them via the selected <see cref="IAuditWriter"/>.
/// </summary>
public sealed class KentosAuditDataProvider : AuditDataProvider
{
    private readonly IServiceScopeFactory _scopeFactory;

    public KentosAuditDataProvider(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public override object InsertEvent(AuditEvent auditEvent) =>
        InsertEventAsync(auditEvent, CancellationToken.None).GetAwaiter().GetResult();

    public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        var efEvent = (auditEvent as AuditEventEntityFramework)?.EntityFrameworkEvent;
        if (efEvent is null || efEvent.Entries.Count == 0)
        {
            return 0;
        }

        using var scope = _scopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        var currentUser = services.GetRequiredService<ICurrentUser>();
        var clock = services.GetRequiredService<TimeProvider>();
        var writer = services.GetRequiredService<IAuditWriter>();
        var now = clock.GetUtcNow();

        var records = new List<AuditLog>(efEvent.Entries.Count);
        foreach (var entry in efEvent.Entries)
        {
            records.Add(new AuditLog
            {
                EntityType = entry.EntityType?.Name ?? entry.Name,
                TableName = entry.Table,
                EntityId = ExtractUuid(entry),
                Module = ExtractModule(entry.EntityType),
                Action = MapAction(entry.Action),
                Changes = (entry.Changes ?? [])
                    .Select(c => new AuditChange(c.ColumnName, c.OriginalValue, c.NewValue))
                    .ToList(),
                IpAddress = currentUser.IpAddress,
                UserId = currentUser.UserId,
                UserName = currentUser.UserName,
                CreatedAt = now,
            });
        }

        await writer.WriteAsync(records, cancellationToken);
        return records.Count;
    }

    // Audit is creation-only (InsertOnEnd); replace is not used.
    public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
    {
    }

    public override Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    private static AuditAction MapAction(string? action) => action switch
    {
        "Insert" => AuditAction.Insert,
        "Update" => AuditAction.Update,
        "Delete" => AuditAction.Delete,
        _ => AuditAction.Update,
    };

    private static string? ExtractUuid(EventEntry entry)
    {
        if (entry.ColumnValues is not null)
        {
            foreach (var key in (string[])["uuid", "Uuid"])
            {
                if (entry.ColumnValues.TryGetValue(key, out var value) && value is not null)
                {
                    return value.ToString();
                }
            }
        }

        return entry.PrimaryKey?.Values.FirstOrDefault()?.ToString();
    }

    private static string? ExtractModule(Type? entityType)
    {
        var ns = entityType?.Namespace;
        if (string.IsNullOrEmpty(ns))
        {
            return null;
        }

        const string marker = "Modules.";
        var index = ns.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
        {
            return null;
        }

        var rest = ns[(index + marker.Length)..];
        return rest.Split('.', 2)[0].ToLowerInvariant();
    }
}
