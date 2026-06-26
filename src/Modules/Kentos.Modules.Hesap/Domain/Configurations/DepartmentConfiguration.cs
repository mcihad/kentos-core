using Kentos.Infrastructure.Persistence;
using Kentos.Modules.Hesap.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kentos.Modules.Hesap.Domain.Configurations;

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departmanlar", t => t.HasComment("Departmanlar (ağaç yapısı)"));
        builder.ConfigureBase();

        builder.Property(e => e.Name).HasColumnName("ad").HasMaxLength(256).HasComment("Departman adı");
        builder.Property(e => e.ParentId).HasColumnName("ust_departman_id").HasComment("Üst departman kimliği (kök için boş)");

        builder.HasOne(e => e.Parent)
            .WithMany(e => e.Children)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.ParentId);
        builder.HasIndex(e => e.Name);
    }
}
