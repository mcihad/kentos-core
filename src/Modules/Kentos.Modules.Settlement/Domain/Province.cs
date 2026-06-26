using Kentos.SharedKernel.Entities;

namespace Kentos.Modules.Settlement.Domain;

/// <summary>A province (mapped to the Turkish 'iller' table).</summary>
public sealed class Province : BaseEntity
{
    public string Name { get; set; } = "";

    public int PlateCode { get; set; }

    public ICollection<District> Districts { get; set; } = new List<District>();
}
