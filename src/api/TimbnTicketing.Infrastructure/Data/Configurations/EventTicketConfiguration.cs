using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimbnTicketing.Core.Entities;

namespace TimbnTicketing.Infrastructure.Data.Configurations;

public class EventTicketConfiguration : IEntityTypeConfiguration<EventTicket>
{
    public void Configure(EntityTypeBuilder<EventTicket> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasOne(e => e.Event)
            .WithMany(ev => ev.EventTickets)
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.TicketType)
            .WithMany(tt => tt.EventTickets)
            .HasForeignKey(e => e.TicketTypeId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
