using Kentos.Infrastructure.Persistence;
using Kentos.Modules.Settlement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kentos.Modules.Settlement.Domain.Configurations;

internal sealed class ProvinceConfiguration : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> builder)
    {
        builder.ToTable("iller", t => t.HasComment("İller"));
        builder.ConfigureBase();

        builder.Property(e => e.Name).HasColumnName("ad").HasMaxLength(128).HasComment("İl adı");
        builder.Property(e => e.PlateCode).HasColumnName("plaka_kodu").HasComment("Plaka kodu (1..81)");

        builder.HasIndex(e => e.PlateCode).IsUnique();
        builder.HasIndex(e => e.Name);

        builder
            .HasMany(e => e.Districts)
            .WithOne(d => d.Province)
            .HasForeignKey(d => d.ProvinceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
