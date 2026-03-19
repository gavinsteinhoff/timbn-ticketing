namespace TimbnTicketing.Core.Entities;

public class UserOrganizationMetadataInfo
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string MetadataName { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsPublic { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<UserOrganizationMetadataValue> Values { get; set; } = [];
}
