using Kentos.Modules.Settlement.Application.Districts;
using Kentos.Modules.Settlement.Domain;
using Mapster;

namespace Kentos.Modules.Settlement.Mappings;

public sealed class DistrictMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<District, DistrictResponse>()
            .Map(dest => dest.Id, src => src.Uuid)
            .Map(dest => dest.ProvinceId, src => src.Province!.Uuid);
    }
}
