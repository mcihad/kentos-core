using Kentos.Modules.Settlement.Application.Neighborhoods;
using Kentos.Modules.Settlement.Application.Provinces;
using Kentos.Modules.Settlement.Domain;
using Kentos.Modules.Settlement.Infrastructure;
using Kentos.Modules.Settlement.Mappings;
using Mapster;
using MapsterMapper;
using Shouldly;

namespace Kentos.Modules.Settlement.UnitTests;

public sealed class MappingTests
{
    private static IMapper BuildMapper()
    {
        var config = new TypeAdapterConfig();
        new ProvinceMapping().Register(config);
        new DistrictMapping().Register(config);
        new NeighborhoodMapping().Register(config);
        config.Compile();
        return new Mapper(config);
    }

    [Fact]
    public void Province_maps_uuid_to_id()
    {
        var province = new Province { Uuid = Guid.NewGuid(), Name = "Istanbul", PlateCode = 34, Version = 3 };

        var response = BuildMapper().Map<ProvinceResponse>(province);

        response.Id.ShouldBe(province.Uuid);
        response.Name.ShouldBe("Istanbul");
        response.PlateCode.ShouldBe(34);
        response.Version.ShouldBe(3);
    }

    [Fact]
    public void Neighborhood_maps_geometry_to_coordinates()
    {
        var district = new District { Uuid = Guid.NewGuid(), Name = "Kadikoy" };
        var neighborhood = new Neighborhood
        {
            Uuid = Guid.NewGuid(),
            Name = "Moda",
            District = district,
            PostalCode = "34710",
            Center = GeometryParser.CreatePoint(40.98, 29.03, 4326),
        };

        var response = BuildMapper().Map<NeighborhoodResponse>(neighborhood);

        response.Id.ShouldBe(neighborhood.Uuid);
        response.DistrictId.ShouldBe(district.Uuid);
        response.Latitude!.Value.ShouldBe(40.98, 0.0001);
        response.Longitude!.Value.ShouldBe(29.03, 0.0001);
    }
}
