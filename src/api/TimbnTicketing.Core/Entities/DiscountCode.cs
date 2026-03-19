namespace TimbnTicketing.Core.Entities;

public class DiscountCode
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? EventId { get; set; }
    public Guid? EventTicketId { get; set; }
    public Guid? UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public int DiscountCents { get; set; }
    public int DiscountPercent { get; set; }
    public int? MaxUses { get; set; }
    public int TimesUsed { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public Event? Event { get; set; }
    public EventTicket? EventTicket { get; set; }
    public User? User { get; set; }
    public ICollection<Order> Orders { get; set; } = [];
}
