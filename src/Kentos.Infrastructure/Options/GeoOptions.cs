namespace Kentos.Infrastructure.Options;

/// <summary>Geospatial (PostGIS) settings.</summary>
public sealed class GeoOptions
{
    public const string SectionName = "Geo";

    /// <summary>Default SRID for geometries (e.g. 4326 = WGS84).</summary>
    public int Srid { get; set; } = 4326;
}
