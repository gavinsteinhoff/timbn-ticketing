using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimbnTicketing.Core;
using TimbnTicketing.Core.Entities;

namespace TimbnTicketing.Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Hierarchy).HasDefaultValue(100);
        builder.Property(e => e.Permissions).HasDefaultValue(Permission.None);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasIndex(e => new { e.OrganizationId, e.Slug }).IsUnique();

        builder.HasOne(e => e.Organization)
            .WithMany(o => o.Roles)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
