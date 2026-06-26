using Kentos.Infrastructure.Persistence;
using Kentos.Modules.Hesap.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kentos.Modules.Hesap.Domain.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("yenileme_tokenlari", t => t.HasComment("Yenileme tokenları (refresh)"));
        builder.ConfigureBase();

        builder.Property(e => e.UserId).HasColumnName("kullanici_id").HasComment("Kullanıcı kimliği");
        builder.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(128).HasComment("Token SHA-256 özeti (base64)");
        builder.Property(e => e.ExpiresAt).HasColumnName("son_kullanma").HasComment("Son kullanma zamanı (UTC)");
        builder.Property(e => e.IsRevoked).HasColumnName("iptal_edildi").HasDefaultValue(false).HasComment("İptal edildi mi");
        builder.Property(e => e.ReplacedById).HasColumnName("yerine_gecen_id").HasComment("Yerine geçen token kimliği (rotasyon)");
        builder.Property(e => e.Ip).HasColumnName("ip").HasMaxLength(64).HasComment("Talep eden istemci IP'si");

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.TokenHash).IsUnique();
    }
}
