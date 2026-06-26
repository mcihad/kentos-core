using Kentos.Infrastructure.Persistence;
using Kentos.Modules.Hesap.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kentos.Modules.Hesap.Domain.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("yetkiler", t => t.HasComment("Yetkiler (sistem tarafından otomatik tanımlanır)"));
        builder.ConfigureBase();

        builder.Property(e => e.Key).HasColumnName("anahtar").HasMaxLength(256).HasComment("Yetki anahtarı (modul.kaynak.eylem)");
        builder.Property(e => e.Module).HasColumnName("modul").HasMaxLength(128).HasComment("Modül slug");
        builder.Property(e => e.Resource).HasColumnName("kaynak").HasMaxLength(128).HasComment("Kaynak adı");
        builder.Property(e => e.Action).HasColumnName("eylem").HasMaxLength(64).HasComment("Eylem adı");
        builder.Property(e => e.Title).HasColumnName("baslik").HasMaxLength(256).HasComment("Görünen başlık");
        builder.Property(e => e.Description).HasColumnName("aciklama").HasMaxLength(512).HasComment("Açıklama");

        builder.HasIndex(e => e.Key).IsUnique();
    }
}
