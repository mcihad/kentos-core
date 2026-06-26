using Kentos.Infrastructure.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kentos.Infrastructure.Persistence.Configurations;

internal sealed class ErrorLogConfiguration : IEntityTypeConfiguration<ErrorLog>
{
    public void Configure(EntityTypeBuilder<ErrorLog> builder)
    {
        builder.ToTable("hata_kayitlari", t => t.HasComment("Yakalanan hata kayıtları (parmak izi ile gruplanır)"));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd().HasComment("Birincil anahtar");

        builder.Property(e => e.Fingerprint).HasColumnName("parmakizi").HasMaxLength(64).HasComment("Hata parmak izi (gruplama anahtarı)");
        builder.HasIndex(e => e.Fingerprint).IsUnique();

        builder.Property(e => e.Origin).HasColumnName("koken").HasConversion<string>().HasMaxLength(16).HasComment("Hata kökeni (Server/Client)");
        builder.Property(e => e.Module).HasColumnName("modul").HasMaxLength(64).HasComment("Hatanın oluştuğu modül slug'ı");
        builder.Property(e => e.Message).HasColumnName("mesaj").HasComment("Hata mesajı");
        builder.Property(e => e.ExceptionType).HasColumnName("istisna_tipi").HasMaxLength(512).HasComment("İstisna tipi (CLR adı)");
        builder.Property(e => e.StackTrace).HasColumnName("yigin_izi").HasComment("Yığın izi (stack trace)");
        builder.Property(e => e.Source).HasColumnName("kaynak").HasMaxLength(512).HasComment("Hata kaynağı (assembly/metot)");
        builder.Property(e => e.FileName).HasColumnName("dosya_adi").HasMaxLength(1024).HasComment("Kaynak dosya adı");
        builder.Property(e => e.LineNumber).HasColumnName("satir_no").HasComment("Kaynak satır numarası");
        builder.Property(e => e.HttpMethod).HasColumnName("http_metot").HasMaxLength(16).HasComment("HTTP metodu");
        builder.Property(e => e.Path).HasColumnName("yol").HasMaxLength(2048).HasComment("İstek yolu");
        builder.Property(e => e.QueryString).HasColumnName("sorgu_dizesi").HasComment("İstek sorgu dizesi");
        builder.Property(e => e.StatusCode).HasColumnName("durum_kodu").HasDefaultValue(500).HasComment("HTTP durum kodu");
        builder.Property(e => e.IpAddress).HasColumnName("ip_adresi").HasMaxLength(64).HasComment("İstemci IP adresi");
        builder.Property(e => e.UserAgent).HasColumnName("istemci_bilgisi").HasComment("İstemci/tarayıcı kimliği (User-Agent)");

        builder.Property(e => e.Headers)
            .HasColumnName("basliklar")
            .HasConversion(JsonbConverters.Json<Dictionary<string, string>>(), JsonbConverters.JsonComparer<Dictionary<string, string>>())
            .HasColumnType("jsonb")
            .HasComment("İstek başlıkları (jsonb)");

        builder.Property(e => e.UserId).HasColumnName("kullanici_id").HasMaxLength(256).HasComment("Kullanıcı kimliği");
        builder.Property(e => e.UserName).HasColumnName("kullanici_adi").HasMaxLength(256).HasComment("Kullanıcı adı");
        builder.Property(e => e.Status).HasColumnName("durum").HasConversion<string>().HasMaxLength(16).HasComment("Triyaj durumu (New/Investigating/Resolved/Ignored)");
        builder.Property(e => e.DeveloperNotes).HasColumnName("gelistirici_notu").HasComment("Geliştirici notu");
        builder.Property(e => e.OccurrenceCount).HasColumnName("tekrar_sayisi").HasDefaultValue(1).HasComment("Aynı hatanın görülme sayısı");
        builder.Property(e => e.FirstSeenAt).HasColumnName("ilk_gorulme").HasComment("İlk görülme zamanı (UTC)");
        builder.Property(e => e.LastSeenAt).HasColumnName("son_gorulme").HasComment("Son görülme zamanı (UTC)");

        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.Origin);
        builder.HasIndex(e => e.LastSeenAt);
    }
}
