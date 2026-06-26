using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kentos.Modules.Hesap.Domain.Configurations;

/// <summary>Turkish table names for the Identity bookkeeping/join tables.</summary>
internal sealed class IdentityUserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<long>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<long>> builder) =>
        builder.ToTable("kullanici_rolleri", t => t.HasComment("Kullanıcı rolleri"));
}

internal sealed class IdentityUserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<long>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<long>> builder) =>
        builder.ToTable("kullanici_iddialari", t => t.HasComment("Kullanıcı iddiaları (claims)"));
}

internal sealed class IdentityRoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<long>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<long>> builder) =>
        builder.ToTable("rol_iddialari", t => t.HasComment("Rol iddiaları (claims)"));
}

internal sealed class IdentityUserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<long>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<long>> builder) =>
        builder.ToTable("kullanici_girisleri", t => t.HasComment("Harici kullanıcı girişleri"));
}

internal sealed class IdentityUserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<long>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<long>> builder) =>
        builder.ToTable("kullanici_tokenlari", t => t.HasComment("Kullanıcı tokenları"));
}
