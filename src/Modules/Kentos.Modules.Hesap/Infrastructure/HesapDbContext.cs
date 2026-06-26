using Kentos.Infrastructure.Persistence;
using Kentos.Modules.Hesap.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kentos.Modules.Hesap.Infrastructure;

/// <summary>
/// The Hesap module DbContext (Turkish 'hesap' schema). Extends ASP.NET Identity's
/// store and also follows the Kentos persistence conventions (soft-delete filter +
/// audit interceptors applied via <c>AddModuleDbContext</c>).
/// </summary>
public sealed class HesapDbContext : IdentityDbContext<
    ApplicationUser, ApplicationRole, long,
    IdentityUserClaim<long>, IdentityUserRole<long>, IdentityUserLogin<long>,
    IdentityRoleClaim<long>, IdentityUserToken<long>>
{
    public const string Schema = "hesap";

    public HesapDbContext(DbContextOptions<HesapDbContext> options) : base(options)
    {
    }

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<UserDepartment> UserDepartments => Set<UserDepartment>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<UserGroupMember> UserGroupMembers => Set<UserGroupMember>();
    public DbSet<AccessPolicy> AccessPolicies => Set<AccessPolicy>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        base.OnModelCreating(modelBuilder); // Identity entity mapping
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HesapDbContext).Assembly);
        modelBuilder.ApplySoftDeleteFilters(); // MUST be last
    }
}
