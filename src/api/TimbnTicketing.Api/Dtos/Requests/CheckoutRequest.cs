namespace TimbnTicketing.Api.Dtos.Requests;

public record CheckoutRequest
{
    public List<CheckoutItemRequest> Items { get; init; } = [];
    public string? DiscountCode { get; init; }
}

public record CheckoutItemRequest(
    Guid EventTicketId,
    Guid? AttendeeUserId = null,
    string? AttendeeEmail = null);
