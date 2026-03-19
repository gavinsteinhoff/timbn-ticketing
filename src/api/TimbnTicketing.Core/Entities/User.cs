namespace TimbnTicketing.Core.Entities;

public class User
{
    public Guid Id { get; set; }
    public string AuthProviderId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<UserOrganization> UserOrganizations { get; set; } = [];
    public ICollection<UserOrganizationMetadataValue> MetadataValues { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<UserTicket> UserTickets { get; set; } = [];
    public ICollection<DiscountCode> OwnedDiscountCodes { get; set; } = [];
}
