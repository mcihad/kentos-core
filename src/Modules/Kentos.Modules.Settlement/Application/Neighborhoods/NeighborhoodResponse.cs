namespace Kentos.Modules.Settlement.Application.Neighborhoods;

public sealed record NeighborhoodResponse(
    Guid Id,
    string Name,
    Guid DistrictId,
    string? PostalCode,
    double? Latitude,
    double? Longitude,
    string? BoundaryWkt,
    long Version,
    DateTimeOffset CreatedAt);
