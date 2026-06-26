using Kentos.SharedKernel.Exceptions;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace Kentos.Modules.Settlement.Infrastructure;

/// <summary>Helpers building SRID-aware NetTopologySuite geometries.</summary>
public static class GeometryParser
{
    public static Point CreatePoint(double latitude, double longitude, int srid)
    {
        var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid);
        return factory.CreatePoint(new Coordinate(longitude, latitude));
    }

    public static MultiPolygon ParseMultiPolygon(string wkt, int srid)
    {
        var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid);
        var reader = new WKTReader(factory);
        var geometry = reader.Read(wkt);

        return geometry switch
        {
            MultiPolygon multiPolygon => multiPolygon,
            Polygon polygon => factory.CreateMultiPolygon([polygon]),
            _ => throw new BusinessRuleException("Boundary must be a Polygon or MultiPolygon WKT."),
        };
    }
}
