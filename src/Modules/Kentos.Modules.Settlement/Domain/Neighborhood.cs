using Kentos.SharedKernel.Entities;
using NetTopologySuite.Geometries;

namespace Kentos.Modules.Settlement.Domain;

/// <summary>A neighborhood (mapped to the Turkish 'mahalleler' table). Showcases PostGIS geometry.</summary>
public sealed class Neighborhood : BaseEntity
{
    public string Name { get; set; } = "";

    public long DistrictId { get; set; }

    public District? District { get; set; }

    public string? PostalCode { get; set; }

    /// <summary>Center point (PostGIS geometry).</summary>
    public Point? Center { get; set; }

    /// <summary>Boundary polygon (PostGIS geometry).</summary>
    public MultiPolygon? Boundary { get; set; }
}
