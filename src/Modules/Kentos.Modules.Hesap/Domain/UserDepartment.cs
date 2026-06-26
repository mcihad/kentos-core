using Kentos.SharedKernel.Entities;

namespace Kentos.Modules.Hesap.Domain;

/// <summary>Membership of a user in a department.</summary>
public sealed class UserDepartment : BaseEntity
{
    public long UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public long DepartmentId { get; set; }
    public Department? Department { get; set; }
}
