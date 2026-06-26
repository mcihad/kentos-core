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

    public string Icon =>
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>
        """;

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
