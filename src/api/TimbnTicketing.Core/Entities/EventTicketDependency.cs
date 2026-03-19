namespace TimbnTicketing.Core.Entities;

public class EventTicketDependency
{
    public Guid Id { get; set; }
    public Guid EventTicketId { get; set; }
    public Guid RequiresEventTicketId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public EventTicket EventTicket { get; set; } = null!;
    public EventTicket RequiresEventTicket { get; set; } = null!;
}
