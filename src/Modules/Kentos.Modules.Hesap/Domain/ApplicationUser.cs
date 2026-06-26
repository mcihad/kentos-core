using Kentos.SharedKernel.Entities;
using Microsoft.AspNetCore.Identity;

namespace Kentos.Modules.Hesap.Domain;

/// <summary>Application user, extending ASP.NET Identity with the Kentos audit/soft-delete contract.</summary>
public sealed class ApplicationUser : IdentityUser<long>, IAuditable, ISoftDeletable
{
    /// <summary>Public UUIDv7 identity ("uuid"); surfaces as "id" in DTOs.</summary>
    public Guid Uuid { get; set; }

    /// <summary>Display name (full name).</summary>
    public string? DisplayName { get; set; }

    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
    public string? DeletedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
