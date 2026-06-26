using Kentos.Modules.Hesap.Application.Users;
using Kentos.Modules.Hesap.Domain;
using Mapster;

namespace Kentos.Modules.Hesap.Mappings;

public sealed class UserMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ApplicationUser, UserResponse>()
            .Map(dest => dest.Id, src => src.Uuid);
    }
}
