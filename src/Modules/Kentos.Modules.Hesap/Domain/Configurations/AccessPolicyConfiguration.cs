using Kentos.Infrastructure.Persistence;
using Kentos.Modules.Hesap.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kentos.Modules.Hesap.Domain.Configurations;

internal sealed class AccessPolicyConfiguration : IEntityTypeConfiguration<AccessPolicy>
{
    public void Configure(EntityTypeBuilder<AccessPolicy> builder)
    {
        builder.ToTable("erisim_politikalari", t => t.HasComment("Erişim politikaları (giriş anında IP/zaman kontrolü)"));
        builder.ConfigureBase();

        builder.Property(e => e.SubjectType)
            .HasColumnName("konu_tipi")
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasComment("Politikanın hedefi: User (kullanıcı) veya Group (grup)");

        builder.Property(e => e.SubjectId).HasColumnName("konu_id").HasComment("Hedef kullanıcı veya grubun dahili kimliği");

        builder.Property(e => e.Kind)
            .HasColumnName("tur")
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasComment("Politika türü: Time (zaman) veya Ip (IP)");

        builder.Property(e => e.Effect)
            .HasColumnName("etki")
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasComment("Etki: Allow (izin) veya Deny (engelle)");

        builder.Property(e => e.Value)
            .HasColumnName("deger")
            .HasMaxLength(128)
            .HasComment("CIDR (IP) veya 'SS:dd-SS:dd' (zaman) değeri");

        builder.Property(e => e.Priority).HasColumnName("oncelik").HasComment("Değerlendirme önceliği (küçük önce)");

        builder.HasIndex(e => new { e.SubjectType, e.SubjectId });
    }
}
