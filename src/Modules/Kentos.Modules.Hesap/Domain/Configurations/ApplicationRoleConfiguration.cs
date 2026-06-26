using Kentos.Modules.Hesap.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kentos.Modules.Hesap.Domain.Configurations;

internal sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("roller", t => t.HasComment("Roller"));

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasComment("Dahili sayısal birincil anahtar (API'de gösterilmez)");

        builder.Property(e => e.Uuid)
            .HasColumnName("uuid")
            .HasDefaultValueSql("uuidv7()")
            .HasComment("Genel UUIDv7 kimlik (API'de 'id' olarak kullanılır)");
        builder.HasIndex(e => e.Uuid).IsUnique();

        builder.Property(e => e.Name).HasColumnName("ad").HasComment("Rol adı");
        builder.Property(e => e.NormalizedName).HasColumnName("normal_ad").HasComment("Normalize edilmiş rol adı");
        builder.Property(e => e.ConcurrencyStamp).HasColumnName("eszamanlilik_damgasi").HasComment("Eşzamanlılık damgası");
        builder.Property(e => e.Description).HasColumnName("aciklama").HasMaxLength(512).HasComment("Rol açıklaması");

        builder.HasMany(e => e.RolePermissions)
            .WithOne(rp => rp.Role!)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ConfigureAuditAndSoftDelete();
    }
}
