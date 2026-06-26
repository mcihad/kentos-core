namespace Kentos.Infrastructure.Auditing;

/// <summary>Persists audit entries to the selected provider (Postgres/Mongo).</summary>
public interface IAuditWriter
{
    Task WriteAsync(IReadOnlyList<AuditLog> records, CancellationToken cancellationToken);
}
