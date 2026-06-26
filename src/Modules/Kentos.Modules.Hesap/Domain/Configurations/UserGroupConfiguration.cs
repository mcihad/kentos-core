using Kentos.Infrastructure.Persistence;
using Kentos.Modules.Hesap.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kentos.Modules.Hesap.Domain.Configurations;

internal sealed class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
{
    public void Configure(EntityTypeBuilder<UserGroup> builder)
    {
        builder.ToTable("kullanici_gruplari", t => t.HasComment("Kullanıcı grupları"));
        builder.ConfigureBase();

        builder.Property(e => e.Name).HasColumnName("ad").HasMaxLength(256).HasComment("Grup adı");
        builder.Property(e => e.Description).HasColumnName("aciklama").HasMaxLength(512).HasComment("Grup açıklaması");

        builder.HasIndex(e => e.Name);
    }
}
