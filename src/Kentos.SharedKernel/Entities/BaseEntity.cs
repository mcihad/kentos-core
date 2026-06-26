namespace Kentos.SharedKernel.Entities;

/// <summary>
/// Base type for all domain entities. Code is English; the Turkish database column
/// names are applied only via EF mapping (HasColumnName) in the infrastructure layer.
/// </summary>
public abstract class BaseEntity : IEntity, IAuditable, ISoftDeletable
{
    /// <summary>bigint identity, internal PK ("id"). Never exposed by the API.</summary>
    public long Id { get; set; }

    /// <summary>Public UUIDv7 identity ("uuid"). Surfaces as "id" in DTOs.</summary>
    public Guid Uuid { get; set; }

    /// <summary>Optimistic concurrency version counter ("surum").</summary>
    public long Version { get; set; }

    /// <summary>Flexible, schema-less metadata stored as jsonb ("meta_veri", camelCase keys).</summary>
    public Dictionary<string, object?> Metadata { get; set; } = new();

    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
    public string? DeletedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
