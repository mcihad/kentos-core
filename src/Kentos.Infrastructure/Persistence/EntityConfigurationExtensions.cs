using Kentos.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kentos.Infrastructure.Persistence;

/// <summary>
/// Common base configuration + mandatory comments invoked by every entity config.
/// Maps the English <see cref="BaseEntity"/> members to their Turkish column names.
/// DB comments are written in Turkish (they describe the Turkish schema).
/// </summary>
public static class EntityConfigurationExtensions
{
    /// <summary>
    /// Configures the <see cref="BaseEntity"/> columns (id, uuid, version, metadata,
    /// audit, soft-delete) with Turkish column names and Turkish comments.
    /// Every entity configuration must call this.
    /// </summary>
    public static void ConfigureBase<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : BaseEntity
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd()
            .HasComment("Dahili sayısal birincil anahtar (API'de gösterilmez)");

        builder.Property(e => e.Uuid)
            .HasColumnName("uuid")
            .HasDefaultValueSql("uuidv7()")
            .HasComment("Genel UUIDv7 kimlik (API'de 'id' olarak kullanılır)");

        builder.HasIndex(e => e.Uuid).IsUnique();

        builder.Property(e => e.Version)
            .HasColumnName("surum")
            .IsConcurrencyToken()
            .HasComment("İyimser eşzamanlılık sürüm sayacı");

        builder.Property(e => e.Metadata)
            .HasColumnName("meta_veri")
            .HasConversion(JsonbConverters.MetadataConverter, JsonbConverters.MetadataComparer)
            .HasColumnType("jsonb")
            .HasComment("Esnek meta veri (jsonb, camelCase anahtarlar)");

        builder.Property(e => e.CreatedBy)
            .HasColumnName("olusturan")
            .HasMaxLength(256)
            .HasComment("Kaydı oluşturan kullanıcı");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("olusturma_tarihi")
            .HasComment("Oluşturma zamanı (UTC)");

        builder.Property(e => e.UpdatedBy)
            .HasColumnName("guncelleyen")
            .HasMaxLength(256)
            .HasComment("Son güncelleyen kullanıcı");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("guncelleme_tarihi")
            .HasComment("Son güncelleme zamanı (UTC)");

        builder.Property(e => e.IsDeleted)
            .HasColumnName("silindi_mi")
            .HasDefaultValue(false)
            .HasComment("Yumuşak silme işareti");

        builder.Property(e => e.DeletedBy)
            .HasColumnName("silen")
            .HasMaxLength(256)
            .HasComment("Silen kullanıcı");

        builder.Property(e => e.DeletedAt)
            .HasColumnName("silme_tarihi")
            .HasComment("Silme zamanı (UTC)");
    }
}
