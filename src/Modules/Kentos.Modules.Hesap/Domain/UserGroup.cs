using Kentos.SharedKernel.Entities;

namespace Kentos.Modules.Hesap.Domain;

/// <summary>A user group. Access policies can target a group as well as an individual user.</summary>
public sealed class UserGroup : BaseEntity
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }

    public ICollection<UserGroupMember> Members { get; set; } = new List<UserGroupMember>();
}
