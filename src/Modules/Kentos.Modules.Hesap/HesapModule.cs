using FluentValidation;
using Kentos.Infrastructure.DependencyInjection;
using Kentos.Modules.Hesap.Access;
using Kentos.Modules.Hesap.Authentication;
using Kentos.Modules.Hesap.Authorization;
using Kentos.Modules.Hesap.Domain;
using Kentos.Modules.Hesap.Infrastructure;
using Kentos.Modules.Hesap.Permissions;
using Kentos.Modules.Hesap.Services;
using Kentos.SharedKernel.Authorization;
using Kentos.SharedKernel.Modules;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kentos.Modules.Hesap;

/// <summary>
/// The Hesap (Account) module: ASP.NET Identity-based users, roles, permissions,
/// departments, groups, access policies, and self-issued JWT authentication. Core
/// module (always enabled) because authentication is fundamental.
/// </summary>
public sealed class HesapModule : IModule
{
    public string Slug => HesapPermissions.ModuleSlug;

    public string DisplayName => "Hesap";

    public string Version => "1.0.0";

    public string? LicenseKey => null; // core module, always enabled

    public IReadOnlyList<PermissionDefinition> Permissions => HesapPermissions.All;

    public void Register(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddModuleDbContext<HesapDbContext>(configuration);
        services.AddValidatorsFromAssembly(typeof(HesapModule).Assembly, includeInternalTypes: true);

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<HesapDbContext>()
            .AddDefaultTokenProviders();

        // Single resolver instance, exposed under both abstractions. Registered here
        // (after the core's deny-all default) so it wins resolution.
        services.AddSingleton<RolePermissionResolver>();
        services.AddSingleton<IPermissionResolver>(sp => sp.GetRequiredService<RolePermissionResolver>());
        services.AddSingleton<IPermissionCacheInvalidator>(sp => sp.GetRequiredService<RolePermissionResolver>());

        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAccessPolicyEvaluator, AccessPolicyEvaluator>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<IPermissionCatalogService, PermissionCatalogService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IUserGroupService, UserGroupService>();
        services.AddScoped<IAccessPolicyService, AccessPolicyService>();
        services.AddScoped<IUserService, UserService>();
    }
}
