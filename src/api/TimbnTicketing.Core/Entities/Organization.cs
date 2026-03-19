namespace TimbnTicketing.Core.Entities;

public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? StripeConnectAccountId { get; set; }
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public bool IsPublic { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Role> Roles { get; set; } = [];
    public ICollection<UserOrganization> UserOrganizations { get; set; } = [];
    public ICollection<UserOrganizationMetadataInfo> MetadataFields { get; set; } = [];
    public ICollection<Venue> Venues { get; set; } = [];
    public ICollection<Event> Events { get; set; } = [];
    public ICollection<TicketType> TicketTypes { get; set; } = [];
    public ICollection<DiscountCode> DiscountCodes { get; set; } = [];
}
