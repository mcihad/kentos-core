using Kentos.Modules.Hesap.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kentos.Modules.Hesap.Domain.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("kullanicilar", t => t.HasComment("Kullanıcılar"));

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasComment("Dahili sayısal birincil anahtar (API'de gösterilmez)");

        builder.Property(e => e.Uuid)
            .HasColumnName("uuid")
            .HasDefaultValueSql("uuidv7()")
            .HasComment("Genel UUIDv7 kimlik (API'de 'id' olarak kullanılır)");
        builder.HasIndex(e => e.Uuid).IsUnique();

        builder.Property(e => e.UserName).HasColumnName("kullanici_adi").HasComment("Kullanıcı adı");
        builder.Property(e => e.NormalizedUserName).HasColumnName("normal_kullanici_adi").HasComment("Normalize edilmiş kullanıcı adı");
        builder.Property(e => e.Email).HasColumnName("e_posta").HasComment("E-posta adresi");
        builder.Property(e => e.NormalizedEmail).HasColumnName("normal_e_posta").HasComment("Normalize edilmiş e-posta");
        builder.Property(e => e.EmailConfirmed).HasColumnName("e_posta_dogrulandi").HasComment("E-posta doğrulandı mı");
        builder.Property(e => e.DisplayName).HasColumnName("ad_soyad").HasMaxLength(256).HasComment("Ad soyad");
        builder.Property(e => e.PasswordHash).HasColumnName("parola_hash").HasComment("Parola özeti");
        builder.Property(e => e.SecurityStamp).HasColumnName("guvenlik_damgasi").HasComment("Güvenlik damgası");
        builder.Property(e => e.ConcurrencyStamp).HasColumnName("eszamanlilik_damgasi").HasComment("Eşzamanlılık damgası");
        builder.Property(e => e.PhoneNumber).HasColumnName("telefon").HasComment("Telefon numarası");
        builder.Property(e => e.PhoneNumberConfirmed).HasColumnName("telefon_dogrulandi").HasComment("Telefon doğrulandı mı");
        builder.Property(e => e.TwoFactorEnabled).HasColumnName("iki_faktor_etkin").HasComment("İki faktörlü doğrulama etkin mi");
        builder.Property(e => e.LockoutEnd).HasColumnName("kilit_bitis").HasComment("Kilit bitiş zamanı");
        builder.Property(e => e.LockoutEnabled).HasColumnName("kilit_etkin").HasComment("Kilitlenebilir mi");
        builder.Property(e => e.AccessFailedCount).HasColumnName("basarisiz_giris_sayisi").HasComment("Ardışık başarısız giriş sayısı");

        builder.ConfigureAuditAndSoftDelete();
    }
}
