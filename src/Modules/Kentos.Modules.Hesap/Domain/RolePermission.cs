using Kentos.SharedKernel.Entities;

namespace Kentos.Modules.Hesap.Domain;

/// <summary>Join between a role and a permission. Role↔permission assignment is user-managed.</summary>
public sealed class RolePermission : BaseEntity
{
    public long RoleId { get; set; }
    public ApplicationRole? Role { get; set; }

    public long PermissionId { get; set; }
    public Permission? Permission { get; set; }
}
