namespace TimbnTicketing.Core.Entities;

public class Event
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? VenueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public string? BannerUrl { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsPublished { get; set; }
    public bool IsPrivate { get; set; }
    public DateTimeOffset? CheckinStartsAt { get; set; }
    public DateTimeOffset? CheckinEndsAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public Venue? Venue { get; set; }
    public ICollection<EventTicket> EventTickets { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<UserTicket> UserTickets { get; set; } = [];
}
