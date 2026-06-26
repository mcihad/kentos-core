using Kentos.Infrastructure.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kentos.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("denetim_kayitlari", t => t.HasComment("Veri katmanı denetim kayıtları"));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd().HasComment("Birincil anahtar");

        builder.Property(e => e.EntityType).HasColumnName("varlik_tipi").HasMaxLength(256).HasComment("Değişen varlık tipi (CLR adı)");
        builder.Property(e => e.EntityId).HasColumnName("varlik_id").HasMaxLength(64).HasComment("Kaydın genel kimliği (Uuid)");
        builder.Property(e => e.TableName).HasColumnName("tablo_adi").HasMaxLength(128).HasComment("Veritabanı tablo adı");
        builder.Property(e => e.Module).HasColumnName("modul").HasMaxLength(64).HasComment("Modül slug'ı");

        builder.Property(e => e.Action)
            .HasColumnName("islem")
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasComment("İşlem türü (Insert/Update/Delete)");

        builder.Property(e => e.Changes)
            .HasColumnName("degisiklikler")
            .HasConversion(JsonbConverters.Json<List<AuditChange>>(), JsonbConverters.JsonComparer<List<AuditChange>>())
            .HasColumnType("jsonb")
            .HasComment("Alan bazlı değişiklikler [{ field, oldValue, newValue }]");

        builder.Property(e => e.IpAddress).HasColumnName("ip_adresi").HasMaxLength(64).HasComment("İstemci IP adresi");
        builder.Property(e => e.UserId).HasColumnName("kullanici_id").HasMaxLength(256).HasComment("İşlemi yapan kullanıcı kimliği");
        builder.Property(e => e.UserName).HasColumnName("kullanici_adi").HasMaxLength(256).HasComment("İşlemi yapan kullanıcı adı");
        builder.Property(e => e.CreatedAt).HasColumnName("olusturma_tarihi").HasComment("Kayıt zamanı (UTC)");

        builder.HasIndex(e => new { e.EntityType, e.EntityId });
        builder.HasIndex(e => e.CreatedAt);
    }
}
