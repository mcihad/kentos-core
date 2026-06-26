using Kentos.SharedKernel.Entities;

namespace Kentos.Modules.Hesap.Domain;

/// <summary>Membership of a user in a user group.</summary>
public sealed class UserGroupMember : BaseEntity
{
    public long GroupId { get; set; }
    public UserGroup? Group { get; set; }

    public long UserId { get; set; }
    public ApplicationUser? User { get; set; }
}
