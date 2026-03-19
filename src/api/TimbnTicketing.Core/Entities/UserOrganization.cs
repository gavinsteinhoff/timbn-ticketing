namespace TimbnTicketing.Core.Entities;

public class UserOrganization
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RoleId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
