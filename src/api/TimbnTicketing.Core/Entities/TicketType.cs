namespace TimbnTicketing.Core.Entities;

public class TicketType
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<EventTicket> EventTickets { get; set; } = [];
}
