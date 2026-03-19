using TimbnTicketing.Core;

namespace TimbnTicketing.Core.Entities;

public class Role
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int Hierarchy { get; set; } = 100;
    public bool IsDefault { get; set; }
    public Permission Permissions { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<UserOrganization> UserOrganizations { get; set; } = [];
}
