namespace TimbnTicketing.Api.Dtos.Responses;

public record EventSummaryResponse(
    Guid Id,
    string Name,
    string Slug,
    string? ShortDescription,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    bool IsPublished,
    bool IsPrivate,
    string? BannerUrl,
    string? AvatarUrl,
    VenueSummaryResponse? Venue);
