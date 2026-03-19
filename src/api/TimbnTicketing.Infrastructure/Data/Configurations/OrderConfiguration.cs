using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimbnTicketing.Core.Entities;

namespace TimbnTicketing.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.StripePaymentIntentId).HasMaxLength(255);
        builder.Property(e => e.StripeCheckoutSessionId).HasMaxLength(255);
        builder.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("pending");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(e => e.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Event)
            .WithMany(ev => ev.Orders)
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.DiscountCode)
            .WithMany(dc => dc.Orders)
            .HasForeignKey(e => e.DiscountCodeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
