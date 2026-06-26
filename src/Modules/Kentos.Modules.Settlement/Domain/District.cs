using Kentos.SharedKernel.Entities;

namespace Kentos.Modules.Settlement.Domain;

/// <summary>A district (mapped to the Turkish 'ilceler' table).</summary>
public sealed class District : BaseEntity
{
    public string Name { get; set; } = "";

    public long ProvinceId { get; set; }

    public Province? Province { get; set; }

    public ICollection<Neighborhood> Neighborhoods { get; set; } = new List<Neighborhood>();
}
