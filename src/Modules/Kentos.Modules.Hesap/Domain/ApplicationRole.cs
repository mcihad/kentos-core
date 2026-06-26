using Kentos.SharedKernel.Entities;
using Microsoft.AspNetCore.Identity;

namespace Kentos.Modules.Hesap.Domain;

/// <summary>Application role. Roles are user-managed (CRUD); the JWT carries role names.</summary>
public sealed class ApplicationRole : IdentityRole<long>, IAuditable, ISoftDeletable
{
    /// <summary>Public UUIDv7 identity ("uuid"); surfaces as "id" in DTOs.</summary>
    public Guid Uuid { get; set; }

    /// <summary>Optional human description of the role.</summary>
    public string? Description { get; set; }

    /// <summary>Permissions granted to this role.</summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
    public string? DeletedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
