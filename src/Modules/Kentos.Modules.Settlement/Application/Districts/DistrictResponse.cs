namespace Kentos.Modules.Settlement.Application.Districts;

public sealed record DistrictResponse(
    Guid Id,
    string Name,
    Guid ProvinceId,
    long Version,
    DateTimeOffset CreatedAt);
