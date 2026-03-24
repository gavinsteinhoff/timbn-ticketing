namespace TimbnTicketing.Api.Dtos.Requests;

public record CreateEventTicketRequest(
    Guid TicketTypeId,
    int PriceCents,
    int? MaxQuantity = null,
    DateTimeOffset? SalesStartAt = null,
    DateTimeOffset? SalesEndAt = null,
    bool RequireAllDependencies = false)
{
    public List<Guid> DependencyEventTicketIds { get; init; } = [];
}
