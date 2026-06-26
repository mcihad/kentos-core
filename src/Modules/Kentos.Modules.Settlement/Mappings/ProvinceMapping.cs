using Kentos.Modules.Settlement.Application.Provinces;
using Kentos.Modules.Settlement.Domain;
using Mapster;

namespace Kentos.Modules.Settlement.Mappings;

public sealed class ProvinceMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Province, ProvinceResponse>()
            .Map(dest => dest.Id, src => src.Uuid);
    }
}
