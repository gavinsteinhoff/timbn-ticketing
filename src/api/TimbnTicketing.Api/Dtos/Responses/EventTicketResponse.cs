namespace TimbnTicketing.Api.Dtos.Responses;

public record EventTicketResponse(
    Guid Id,
    Guid EventId,
    TicketTypeResponse TicketType,
    int PriceCents,
    int? MaxQuantity,
    DateTimeOffset? SalesStartAt,
    DateTimeOffset? SalesEndAt,
    bool RequireAllDependencies,
    bool IsActive,
    List<Guid> DependencyEventTicketIds,
    DateTimeOffset CreatedAt);

public record TicketTypeResponse(
    Guid Id,
    string Name,
    string? Description);
