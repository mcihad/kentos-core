using Kentos.SharedKernel.Entities;

namespace Kentos.Modules.Hesap.Domain;

/// <summary>
/// A permission known to the system. Permissions are auto-defined at startup from every
/// module's declared <see cref="Kentos.SharedKernel.Authorization.PermissionDefinition"/>;
/// they are never created by users. The unique <see cref="Key"/> is <c>module.resource.action</c>.
/// </summary>
public sealed class Permission : BaseEntity
{
    public string Key { get; set; } = "";
    public string Module { get; set; } = "";
    public string Resource { get; set; } = "";
    public string Action { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }

    /// <summary>Roles this permission is granted to.</summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
