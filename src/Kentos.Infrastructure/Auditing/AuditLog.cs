namespace Kentos.Infrastructure.Auditing;

/// <summary>
/// A data-layer audit entry: one tracked entity mutation. English type, mapped to
/// the Turkish 'denetim_kayitlari' table. Excluded from auditing (no recursion).
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    /// <summary>Mutated entity type (CLR name).</summary>
    public string EntityType { get; set; } = "";

    /// <summary>Public identity (Uuid) of the mutated record.</summary>
    public string? EntityId { get; set; }

    /// <summary>Database table name.</summary>
    public string? TableName { get; set; }

    /// <summary>Module slug.</summary>
    public string? Module { get; set; }

    public AuditAction Action { get; set; }

    /// <summary>Field-level changes.</summary>
    public List<AuditChange> Changes { get; set; } = new();

    public string? IpAddress { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }

    /// <summary>Record timestamp (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
