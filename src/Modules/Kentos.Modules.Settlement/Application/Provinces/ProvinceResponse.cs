namespace Kentos.Modules.Settlement.Application.Provinces;

public sealed record ProvinceResponse(
    Guid Id,
    string Name,
    int PlateCode,
    long Version,
    DateTimeOffset CreatedAt);
