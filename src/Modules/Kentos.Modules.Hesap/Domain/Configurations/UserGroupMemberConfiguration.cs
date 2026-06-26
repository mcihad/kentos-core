using Kentos.Infrastructure.Persistence;
using Kentos.Modules.Hesap.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kentos.Modules.Hesap.Domain.Configurations;

internal sealed class UserGroupMemberConfiguration : IEntityTypeConfiguration<UserGroupMember>
{
    public void Configure(EntityTypeBuilder<UserGroupMember> builder)
    {
        builder.ToTable("kullanici_grup_uyeleri", t => t.HasComment("Kullanıcı grup üyelikleri"));
        builder.ConfigureBase();

        builder.Property(e => e.GroupId).HasColumnName("grup_id").HasComment("Grup kimliği");
        builder.Property(e => e.UserId).HasColumnName("kullanici_id").HasComment("Kullanıcı kimliği");

        builder.HasOne(e => e.Group)
            .WithMany(g => g.Members)
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.GroupId, e.UserId }).IsUnique();
    }
}
