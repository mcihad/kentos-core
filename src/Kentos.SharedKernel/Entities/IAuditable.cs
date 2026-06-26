namespace Kentos.SharedKernel.Entities;

/// <summary>Entity carrying create/update audit fields.</summary>
public interface IAuditable
{
    string? CreatedBy { get; set; }
    DateTimeOffset CreatedAt { get; set; }
    string? UpdatedBy { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
}
