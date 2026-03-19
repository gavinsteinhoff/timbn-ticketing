using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimbnTicketing.Core.Entities;

namespace TimbnTicketing.Infrastructure.Data.Configurations;

public class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
{
    public void Configure(EntityTypeBuilder<DiscountCode> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).HasMaxLength(100).IsRequired();
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasIndex(e => new { e.OrganizationId, e.Code }).IsUnique();

        builder.HasOne(e => e.Organization)
            .WithMany(o => o.DiscountCodes)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.EventTicket)
            .WithMany()
            .HasForeignKey(e => e.EventTicketId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.User)
            .WithMany(u => u.OwnedDiscountCodes)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
