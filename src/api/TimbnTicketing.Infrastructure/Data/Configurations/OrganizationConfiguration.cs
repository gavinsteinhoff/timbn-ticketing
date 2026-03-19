using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimbnTicketing.Core.Entities;

namespace TimbnTicketing.Infrastructure.Data.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(100).IsRequired();
        builder.Property(e => e.StripeConnectAccountId).HasMaxLength(255);
        builder.Property(e => e.LogoUrl).HasMaxLength(500);
        builder.Property(e => e.WebsiteUrl).HasMaxLength(500);
        builder.Property(e => e.IsPublic).HasDefaultValue(false);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasIndex(e => e.Slug).IsUnique();
    }
}
