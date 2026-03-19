using TimbnTicketing.Core;

namespace TimbnTicketing.Api.Auth;

/// <summary>
/// Scoped service that holds the authenticated user's platform ID,
/// resolved from the JWT "sub" claim (AuthProviderId) during authentication.
/// Also holds org context and permissions resolved by middleware.
/// </summary>
public class CurrentUserContext
{
    private Permission _permissions = Permission.None;

    public Guid UserId { get; set; }
    public string AuthProviderId { get; set; } = string.Empty;
    public bool IsAuthenticated => UserId != Guid.Empty;

    // Set by OrgResolutionMiddleware
    public Guid? OrganizationId { get; set; }
    public bool IsOrgPublic { get; set; }

    // Set by MembershipResolutionMiddleware
    public Guid? RoleId { get; set; }
    public int? RoleHierarchy { get; set; }

    public bool IsOrgScoped => OrganizationId.HasValue;
    public bool IsMember => RoleId.HasValue;
    public bool CanViewOrg => IsMember || IsOrgPublic;

    public Permission Permissions => _permissions;

    public bool HasPermission(Permission permission) => _permissions.HasFlag(permission);

    public void GrantPermissions(Permission permissions)
    {
        _permissions |= permissions;
    }
}
