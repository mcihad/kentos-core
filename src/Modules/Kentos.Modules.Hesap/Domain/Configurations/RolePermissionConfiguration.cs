using Kentos.Infrastructure.Persistence;
using Kentos.Modules.Hesap.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kentos.Modules.Hesap.Domain.Configurations;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("rol_yetkileri", t => t.HasComment("Rol yetkileri (rol ↔ yetki ataması)"));
        builder.ConfigureBase();

        builder.Property(e => e.RoleId).HasColumnName("rol_id").HasComment("Rol kimliği");
        builder.Property(e => e.PermissionId).HasColumnName("yetki_id").HasComment("Yetki kimliği");

        builder.HasOne(e => e.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(e => e.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
    }
}
