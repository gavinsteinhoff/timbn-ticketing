namespace TimbnTicketing.Api.Dtos.Responses;

public record OrganizationResponse(
    Guid Id,
    string Name,
    string Slug,
    string? LogoUrl,
    string? WebsiteUrl);
