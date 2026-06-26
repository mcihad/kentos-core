using Kentos.Modules.Hesap.Application.Permissions;
using Kentos.Modules.Hesap.Domain;
using Mapster;

namespace Kentos.Modules.Hesap.Mappings;

public sealed class PermissionMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Permission, PermissionResponse>()
            .Map(dest => dest.Id, src => src.Uuid);
    }
}
