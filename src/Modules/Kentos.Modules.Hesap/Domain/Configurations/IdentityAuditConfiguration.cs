using Kentos.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kentos.Modules.Hesap.Domain.Configurations;

/// <summary>
/// Maps the audit + soft-delete columns (Turkish names/comments) for the Identity
/// entities, which cannot inherit <see cref="BaseEntity"/> so cannot use
/// <c>ConfigureBase</c>. Mirrors the column names used there exactly.
/// </summary>
internal static class IdentityAuditConfiguration
{
    public static void ConfigureAuditAndSoftDelete<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IAuditable, ISoftDeletable
    {
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
