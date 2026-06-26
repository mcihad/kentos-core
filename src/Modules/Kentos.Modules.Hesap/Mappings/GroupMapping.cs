using Kentos.Modules.Hesap.Application.Groups;
using Kentos.Modules.Hesap.Domain;
using Mapster;

namespace Kentos.Modules.Hesap.Mappings;

public sealed class GroupMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UserGroup, GroupResponse>()
            .Map(dest => dest.Id, src => src.Uuid)
            .Map(dest => dest.MemberCount, src => src.Members.Count);
    }
}
