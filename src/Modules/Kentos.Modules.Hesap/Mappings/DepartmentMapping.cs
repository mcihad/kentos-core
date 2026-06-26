using Kentos.Modules.Hesap.Application.Departments;
using Kentos.Modules.Hesap.Domain;
using Mapster;

namespace Kentos.Modules.Hesap.Mappings;

public sealed class DepartmentMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Department, DepartmentResponse>()
            .Map(dest => dest.Id, src => src.Uuid)
            .Map(dest => dest.ParentId, src => src.Parent != null ? (Guid?)src.Parent.Uuid : null);
    }
}
