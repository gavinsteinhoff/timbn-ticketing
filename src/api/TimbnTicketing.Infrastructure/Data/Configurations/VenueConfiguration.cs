using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimbnTicketing.Core.Entities;

namespace TimbnTicketing.Infrastructure.Data.Configurations;

public class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Address).HasMaxLength(500).IsRequired();
        builder.Property(e => e.City).HasMaxLength(100);
        builder.Property(e => e.State).HasMaxLength(50);
        builder.Property(e => e.Zip).HasMaxLength(20);
        builder.Property(e => e.MapUrl).HasMaxLength(500);
        builder.Property(e => e.WebsiteUrl).HasMaxLength(500);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(e => e.Organization)
            .WithMany(o => o.Venues)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
