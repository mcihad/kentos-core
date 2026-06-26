using Kentos.Modules.Settlement.Application.Neighborhoods;
using Kentos.Modules.Settlement.Domain;
using Mapster;

namespace Kentos.Modules.Settlement.Mappings;

public sealed class NeighborhoodMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Neighborhood, NeighborhoodResponse>()
            .Map(dest => dest.Id, src => src.Uuid)
            .Map(dest => dest.DistrictId, src => src.District!.Uuid)
            .Map(dest => dest.Latitude, src => src.Center != null ? (double?)src.Center.Y : null)
            .Map(dest => dest.Longitude, src => src.Center != null ? (double?)src.Center.X : null)
            .Map(dest => dest.BoundaryWkt, src => src.Boundary != null ? src.Boundary.AsText() : null);
    }
}
