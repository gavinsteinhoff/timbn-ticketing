namespace TimbnTicketing.Core.Entities;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid EventTicketId { get; set; }
    public int PriceCents { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Order Order { get; set; } = null!;
    public EventTicket EventTicket { get; set; } = null!;
    public UserTicket? UserTicket { get; set; }
}
