using Kentos.Infrastructure.Persistence;
using Kentos.Modules.Settlement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kentos.Modules.Settlement.Domain.Configurations;

internal sealed class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> builder)
    {
        builder.ToTable("ilceler", t => t.HasComment("İlçeler"));
        builder.ConfigureBase();

        builder.Property(e => e.Name).HasColumnName("ad").HasMaxLength(128).HasComment("İlçe adı");
        builder.Property(e => e.ProvinceId).HasColumnName("il_id").HasComment("İl yabancı anahtarı");

        builder.HasIndex(e => new { e.ProvinceId, e.Name }).IsUnique();

        builder
            .HasMany(e => e.Neighborhoods)
            .WithOne(n => n.District)
            .HasForeignKey(n => n.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
