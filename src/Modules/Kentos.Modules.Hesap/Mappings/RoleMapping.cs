using Kentos.Modules.Hesap.Application.Roles;
using Kentos.Modules.Hesap.Domain;
using Mapster;

namespace Kentos.Modules.Hesap.Mappings;

public sealed class RoleMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ApplicationRole, RoleResponse>()
            .Map(dest => dest.Id, src => src.Uuid)
            .Map(dest => dest.PermissionCount, src => src.RolePermissions.Count);

        config.NewConfig<ApplicationRole, RoleDetailResponse>()
            .Map(dest => dest.Id, src => src.Uuid)
            .Map(dest => dest.Permissions, src => src.RolePermissions.Select(rp => rp.Permission!.Key));
    }
}
