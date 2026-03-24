namespace TimbnTicketing.Api.Dtos.Responses;

public record VenueSummaryResponse(
    Guid Id,
    string Name,
    string Address,
    string? City,
    string? State);
