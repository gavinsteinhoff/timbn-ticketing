namespace TimbnTicketing.Api.Dtos.Responses;

public record CurrentUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    DateTimeOffset CreatedAt,
    List<CurrentUserOrganizationResponse> Organizations);

public record CurrentUserOrganizationResponse(
    Guid OrgId,
    string OrgSlug,
    string OrgName,
    CurrentUserRoleResponse Role,
    Dictionary<string, string> Metadata);

public record CurrentUserRoleResponse(
    Guid Id,
    string Slug,
    string Name);
