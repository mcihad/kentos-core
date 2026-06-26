using Kentos.Infrastructure.Persistence;

namespace Kentos.Infrastructure.Auditing;

/// <summary>Writes audit entries to the Postgres 'denetim' schema (dev default).</summary>
public sealed class PostgresAuditWriter : IAuditWriter
{
    private readonly AuditingDbContext _db;

    public PostgresAuditWriter(AuditingDbContext db) => _db = db;

    public async Task WriteAsync(IReadOnlyList<AuditLog> records, CancellationToken cancellationToken)
    {
        if (records.Count == 0)
        {
            return;
        }

        _db.AuditLogs.AddRange(records);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
