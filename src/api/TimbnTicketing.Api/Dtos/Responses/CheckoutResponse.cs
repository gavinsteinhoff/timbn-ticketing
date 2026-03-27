namespace TimbnTicketing.Api.Dtos.Responses;

public record CheckoutResponse(
    string CheckoutUrl,
    string SessionId,
    DateTimeOffset ExpiresAt);
