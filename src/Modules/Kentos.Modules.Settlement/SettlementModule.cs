using FluentValidation;
using Kentos.Infrastructure.DependencyInjection;
using Kentos.Modules.Settlement.Infrastructure;
using Kentos.Modules.Settlement.Permissions;
using Kentos.Modules.Settlement.Services;
using Kentos.SharedKernel.Authorization;
using Kentos.SharedKernel.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kentos.Modules.Settlement;

/// <summary>The Settlement module: provinces, districts and neighborhoods.</summary>
public sealed class SettlementModule : IModule
{
    public string Slug => SettlementPermissions.ModuleSlug;

    public string DisplayName => "Yerleşim";

    public string Version => "1.0.0";

    public string? LicenseKey => "settlement";

    public IReadOnlyList<PermissionDefinition> Permissions => SettlementPermissions.All;

    public void Register(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddModuleDbContext<SettlementDbContext>(configuration);
        services.AddValidatorsFromAssembly(typeof(SettlementModule).Assembly, includeInternalTypes: true);

        services.AddScoped<IProvinceService, ProvinceService>();
        services.AddScoped<IDistrictService, DistrictService>();
        services.AddScoped<INeighborhoodService, NeighborhoodService>();
    }
}
