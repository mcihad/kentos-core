using Kentos.Infrastructure.Persistence;
using Kentos.Modules.Settlement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kentos.Modules.Settlement.Domain.Configurations;

internal sealed class NeighborhoodConfiguration : IEntityTypeConfiguration<Neighborhood>
{
    public void Configure(EntityTypeBuilder<Neighborhood> builder)
    {
        builder.ToTable("mahalleler", t => t.HasComment("Mahalleler"));
        builder.ConfigureBase();

        builder.Property(e => e.Name).HasColumnName("ad").HasMaxLength(128).HasComment("Mahalle adı");
        builder.Property(e => e.DistrictId).HasColumnName("ilce_id").HasComment("İlçe yabancı anahtarı");
        builder.Property(e => e.PostalCode).HasColumnName("posta_kodu").HasMaxLength(16).HasComment("Posta kodu");

        builder.Property(e => e.Center)
            .HasColumnName("merkez")
            .HasColumnType("geometry(Point, 4326)")
            .HasComment("Merkez nokta (SRID 4326)");

        builder.Property(e => e.Boundary)
            .HasColumnName("sinir")
            .HasColumnType("geometry(MultiPolygon, 4326)")
            .HasComment("Sınır çokgeni (SRID 4326)");

        builder.HasIndex(e => new { e.DistrictId, e.Name }).IsUnique();
    }
}
