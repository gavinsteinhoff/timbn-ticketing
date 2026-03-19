using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimbnTicketing.Core.Entities;

namespace TimbnTicketing.Infrastructure.Data.Configurations;

public class UserOrganizationMetadataInfoConfiguration : IEntityTypeConfiguration<UserOrganizationMetadataInfo>
{
    public void Configure(EntityTypeBuilder<UserOrganizationMetadataInfo> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.MetadataName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.DisplayLabel).HasMaxLength(100).IsRequired();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasIndex(e => new { e.OrganizationId, e.MetadataName }).IsUnique();

        builder.HasOne(e => e.Organization)
            .WithMany(o => o.MetadataFields)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
