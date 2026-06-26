using Kentos.Infrastructure.Persistence;
using Kentos.Modules.Hesap.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kentos.Modules.Hesap.Domain.Configurations;

internal sealed class UserDepartmentConfiguration : IEntityTypeConfiguration<UserDepartment>
{
    public void Configure(EntityTypeBuilder<UserDepartment> builder)
    {
        builder.ToTable("kullanici_departmanlari", t => t.HasComment("Kullanıcı departman üyelikleri"));
        builder.ConfigureBase();

        builder.Property(e => e.UserId).HasColumnName("kullanici_id").HasComment("Kullanıcı kimliği");
        builder.Property(e => e.DepartmentId).HasColumnName("departman_id").HasComment("Departman kimliği");

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Department)
            .WithMany(d => d.Members)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.DepartmentId }).IsUnique();
    }
}
