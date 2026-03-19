namespace TimbnTicketing.Core.Entities;

public class EventTicket
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid TicketTypeId { get; set; }
    public int PriceCents { get; set; }
    public int? MaxQuantity { get; set; }
    public DateTimeOffset? SalesStartAt { get; set; }
    public DateTimeOffset? SalesEndAt { get; set; }
    public bool RequireAllDependencies { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    public Event Event { get; set; } = null!;
    public TicketType TicketType { get; set; } = null!;
    public ICollection<EventTicketDependency> Dependencies { get; set; } = [];
    public ICollection<EventTicketDependency> DependedOnBy { get; set; } = [];
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
