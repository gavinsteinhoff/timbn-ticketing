using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimbnTicketing.Core.Entities;

namespace TimbnTicketing.Infrastructure.Data.Configurations;

public class EventTicketDependencyConfiguration : IEntityTypeConfiguration<EventTicketDependency>
{
    public void Configure(EntityTypeBuilder<EventTicketDependency> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasIndex(e => new { e.EventTicketId, e.RequiresEventTicketId }).IsUnique();

        builder.HasOne(e => e.EventTicket)
            .WithMany(et => et.Dependencies)
            .HasForeignKey(e => e.EventTicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.RequiresEventTicket)
            .WithMany(et => et.DependedOnBy)
            .HasForeignKey(e => e.RequiresEventTicketId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
