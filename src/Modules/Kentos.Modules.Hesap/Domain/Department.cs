using Kentos.SharedKernel.Entities;

namespace Kentos.Modules.Hesap.Domain;

/// <summary>An organizational department, forming a self-referencing tree.</summary>
public sealed class Department : BaseEntity
{
    public string Name { get; set; } = "";

    /// <summary>Parent department; null for a root node.</summary>
    public long? ParentId { get; set; }
    public Department? Parent { get; set; }

    public ICollection<Department> Children { get; set; } = new List<Department>();
    public ICollection<UserDepartment> Members { get; set; } = new List<UserDepartment>();
}
