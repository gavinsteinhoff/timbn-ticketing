namespace TimbnTicketing.Api.Dtos.Responses;

public record MemberSearchResponse(List<MemberSearchResult> Data);

public record MemberSearchResult(
    Guid UserId,
    string FirstName,
    string LastName,
    Dictionary<string, string> Metadata);
