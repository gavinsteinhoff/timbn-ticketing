namespace TimbnTicketing.Core.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid EventId { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? StripeCheckoutSessionId { get; set; }
    public string Status { get; set; } = "pending";
    public int TotalCents { get; set; }
    public int PlatformFeeCents { get; set; }
    public Guid? DiscountCodeId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public DiscountCode? DiscountCode { get; set; }
    public ICollection<OrderItem> Items { get; set; } = [];
}
