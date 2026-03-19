namespace TimbnTicketing.Core.Entities;

public class UserTicket
{
    public Guid Id { get; set; }
    public Guid OrderItemId { get; set; }
    public Guid? UserId { get; set; }
    public Guid EventId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string Status { get; set; } = "valid";
    public string? ClaimEmail { get; set; }
    public string? ClaimToken { get; set; }
    public DateTimeOffset? ClaimExpiresAt { get; set; }
    public DateTimeOffset? CheckedInAt { get; set; }
    public Guid? CheckedInByStaffId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public OrderItem OrderItem { get; set; } = null!;
    public User? User { get; set; }
    public Event Event { get; set; } = null!;
    public User? CheckedInByStaff { get; set; }
}
