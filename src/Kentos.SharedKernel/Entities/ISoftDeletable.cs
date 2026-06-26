namespace Kentos.SharedKernel.Entities;

/// <summary>Entity carrying soft-delete fields.</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    string? DeletedBy { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
}
