namespace TimbnTicketing.Core.Entities;

public class Venue
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? Notes { get; set; }
    public int? Capacity { get; set; }
    public string? MapUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<Event> Events { get; set; } = [];
}
