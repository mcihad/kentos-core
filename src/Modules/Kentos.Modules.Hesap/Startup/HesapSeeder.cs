using Kentos.Infrastructure.Modules;
using Kentos.Modules.Hesap.Domain;
using Kentos.Modules.Hesap.Infrastructure;
using Kentos.SharedKernel.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kentos.Modules.Hesap.Startup;

/// <summary>
/// Idempotent startup seeding for the Hesap module: upserts the permission catalog from
/// every enabled module, and bootstraps an administrator role + user. Permissions are
/// system-defined (never user-created); roles otherwise stay user-managed.
/// </summary>
public static class HesapSeeder
{
    public const string AdminRoleName = "yonetici";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<HesapDbContext>();
        var registry = sp.GetRequiredService<ModuleRegistry>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("HesapSeeder");

        await UpsertPermissionsAsync(db, registry, cancellationToken);
        await BootstrapAdminAsync(sp, db, logger, cancellationToken);

        // Refresh the role → permission cache so the first request sees seeded grants.
        sp.GetService<IPermissionCacheInvalidator>()?.Invalidate();
    }

    private static async Task UpsertPermissionsAsync(
        HesapDbContext db, ModuleRegistry registry, CancellationToken cancellationToken)
    {
        var defined = registry.EnabledModules
            .SelectMany(m => m.Permissions)
            .GroupBy(p => p.Key)
            .Select(g => g.First())
            .ToList();

        var existing = await db.Permissions.ToDictionaryAsync(p => p.Key, cancellationToken);

        foreach (var def in defined)
        {
            if (existing.TryGetValue(def.Key, out var current))
            {
                current.Module = def.Module;
                current.Resource = def.Resource;
                current.Action = def.Action;
                current.Title = def.Title;
                current.Description = def.Description;
            }
            else
            {
                db.Permissions.Add(new Permission
                {
                    Key = def.Key,
                    Module = def.Module,
                    Resource = def.Resource,
                    Action = def.Action,
                    Title = def.Title,
                    Description = def.Description,
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task BootstrapAdminAsync(
        IServiceProvider sp, HesapDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var config = sp.GetRequiredService<IConfiguration>();

        var role = await roleManager.FindByNameAsync(AdminRoleName);
        if (role is null)
        {
            role = new ApplicationRole { Name = AdminRoleName, Description = "Tüm yetkilere sahip sistem yöneticisi" };
            var created = await roleManager.CreateAsync(role);
            if (!created.Succeeded)
            {
                logger.LogError("Admin rolü oluşturulamadı: {Errors}", Describe(created));
                return;
            }
        }

        // Ensure the admin role holds every permission.
        var permissionIds = await db.Permissions.Select(p => p.Id).ToListAsync(cancellationToken);
        var assigned = await db.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);

        var missing = permissionIds.Except(assigned).ToList();
        if (missing.Count > 0)
        {
            db.RolePermissions.AddRange(missing.Select(id => new RolePermission { RoleId = role.Id, PermissionId = id }));
            await db.SaveChangesAsync(cancellationToken);
        }

        // Bootstrap admin user.
        var userName = config["Hesap:Bootstrap:UserName"] ?? "admin";
        var password = config["Hesap:Bootstrap:Password"] ?? "Admin!234";
        var email = config["Hesap:Bootstrap:Email"] ?? "admin@kentos.local";

        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                DisplayName = "Sistem Yöneticisi",
            };

            var created = await userManager.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                logger.LogError("Yönetici kullanıcı oluşturulamadı: {Errors}", Describe(created));
                return;
            }

            logger.LogInformation("Bootstrap yönetici kullanıcı oluşturuldu: {UserName}", userName);
        }

        if (!await userManager.IsInRoleAsync(user, AdminRoleName))
        {
            await userManager.AddToRoleAsync(user, AdminRoleName);
        }
    }

    private static string Describe(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(e => $"{e.Code}:{e.Description}"));
}
