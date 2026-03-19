using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimbnTicketing.Core.Entities;

namespace TimbnTicketing.Infrastructure.Data.Configurations;

public class UserOrganizationMetadataValueConfiguration : IEntityTypeConfiguration<UserOrganizationMetadataValue>
{
    public void Configure(EntityTypeBuilder<UserOrganizationMetadataValue> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.MetadataValue).HasMaxLength(500).IsRequired();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasIndex(e => new { e.UserId, e.MetadataInfoId }).IsUnique();

        builder.HasOne(e => e.User)
            .WithMany(u => u.MetadataValues)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.MetadataInfo)
            .WithMany(m => m.Values)
            .HasForeignKey(e => e.MetadataInfoId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
