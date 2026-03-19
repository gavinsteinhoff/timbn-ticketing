namespace TimbnTicketing.Core.Entities;

public class UserOrganizationMetadataValue
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid MetadataInfoId { get; set; }
    public string MetadataValue { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public UserOrganizationMetadataInfo MetadataInfo { get; set; } = null!;
}
