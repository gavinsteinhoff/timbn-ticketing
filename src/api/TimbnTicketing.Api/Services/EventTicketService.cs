using Microsoft.EntityFrameworkCore;
using TimbnTicketing.Api.Dtos.Requests;
using TimbnTicketing.Api.Dtos.Responses;
using TimbnTicketing.Core.Entities;
using TimbnTicketing.Core.Interfaces;
using TimbnTicketing.Infrastructure.Data;

namespace TimbnTicketing.Api.Services;

public class EventTicketService(PlatformDbContext db, IStripeProductService stripeProductService)
{
    public async Task<EventTicketResponse?> CreateAsync(
        Guid organizationId,
        Guid eventId,
        string? stripeConnectAccountId,
        string? eventName,
        CreateEventTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var ticketType = await db.TicketTypes
            .FirstOrDefaultAsync(t => t.Id == request.TicketTypeId && t.OrganizationId == organizationId, cancellationToken);

        if (ticketType is null)
            return null;

        var eventTicket = new EventTicket
        {
            EventId = eventId,
            TicketTypeId = request.TicketTypeId,
            PriceCents = request.PriceCents,
            MaxQuantity = request.MaxQuantity,
            SalesStartAt = request.SalesStartAt,
            SalesEndAt = request.SalesEndAt,
            RequireAllDependencies = request.RequireAllDependencies,
        };

        if (request.DependencyEventTicketIds.Count > 0)
        {
            eventTicket.Dependencies = request.DependencyEventTicketIds
                .Select(depId => new EventTicketDependency
                {
                    RequiresEventTicketId = depId,
                })
                .ToList();
        }

        if (stripeConnectAccountId is not null)
        {
            var productName = $"{ticketType.Name} - {eventName}";
            var result = await stripeProductService.CreateProductAsync(
                stripeConnectAccountId,
                productName,
                ticketType.Description,
                request.PriceCents,
                cancellationToken);

            eventTicket.StripeProductId = result.ProductId;
            eventTicket.StripePriceId = result.PriceId;
        }

        db.EventTickets.Add(eventTicket);
        await db.SaveChangesAsync(cancellationToken);

        return new EventTicketResponse(
            eventTicket.Id,
            eventTicket.EventId,
            new TicketTypeResponse(ticketType.Id, ticketType.Name, ticketType.Description),
            eventTicket.PriceCents,
            eventTicket.MaxQuantity,
            eventTicket.SalesStartAt,
            eventTicket.SalesEndAt,
            eventTicket.RequireAllDependencies,
            eventTicket.IsActive,
            request.DependencyEventTicketIds,
            eventTicket.CreatedAt);
    }
}
