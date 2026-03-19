using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimbnTicketing.Core.Entities;

namespace TimbnTicketing.Infrastructure.Data.Configurations;

public class UserTicketConfiguration : IEntityTypeConfiguration<UserTicket>
{
    public void Configure(EntityTypeBuilder<UserTicket> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TicketCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("valid");
        builder.Property(e => e.ClaimEmail).HasMaxLength(255);
        builder.Property(e => e.ClaimToken).HasMaxLength(255);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasIndex(e => e.TicketCode).IsUnique();
        builder.HasIndex(e => e.ClaimToken).IsUnique().HasFilter("[ClaimToken] IS NOT NULL");

        builder.HasOne(e => e.OrderItem)
            .WithOne(oi => oi.UserTicket)
            .HasForeignKey<UserTicket>(e => e.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany(u => u.UserTickets)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Event)
            .WithMany(ev => ev.UserTickets)
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.CheckedInByStaff)
            .WithMany()
            .HasForeignKey(e => e.CheckedInByStaffId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
