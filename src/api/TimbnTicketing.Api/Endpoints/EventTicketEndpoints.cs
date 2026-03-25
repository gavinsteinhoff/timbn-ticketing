using TimbnTicketing.Api.Auth;
using TimbnTicketing.Api.Dtos.Requests;
using TimbnTicketing.Api.Dtos.Responses;
using TimbnTicketing.Api.Services;
using TimbnTicketing.Core;

namespace TimbnTicketing.Api.Endpoints;

public static class EventTicketEndpoints
{
    public static RouteGroupBuilder MapEventTicketEndpoints(this RouteGroupBuilder group)
    {
        group.WithTags("Event Tickets");

        group.MapGet("/", HandleListEventTickets);
        group.MapPost("/", HandleCreateEventTicket)
            .WithName("CreateEventTicket")
            .WithSummary("Create a ticket offering for an event")
            .WithDescription("Creates an event ticket with pricing, capacity, and optional dependencies. Syncs to Stripe if the org has a connected account.")
            .RequirePermission(Permission.CanManageEvents)
            .Accepts<CreateEventTicketRequest>("application/json")
            .Produces<EventTicketResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPatch("/{eventTicketId:guid}", HandleUpdateEventTicket)
            .RequirePermission(Permission.CanManageEvents);
        group.MapDelete("/{eventTicketId:guid}", HandleDeleteEventTicket)
            .RequirePermission(Permission.CanManageEvents);

        return group;
    }

    private static Task<IResult> HandleListEventTickets(string orgSlug, string eventSlug) => throw new NotImplementedException();

    private static async Task<IResult> HandleCreateEventTicket(
        string orgSlug,
        string eventSlug,
        CreateEventTicketRequest request,
        EventTicketService eventTicketService,
        CurrentRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        var result = await eventTicketService.CreateAsync(
            requestContext.OrganizationId!.Value,
            requestContext.EventId!.Value,
            requestContext.OrgStripeConnectAccountId,
            requestContext.EventName,
            request,
            cancellationToken);

        return result is not null
            ? Results.Created($"/orgs/{orgSlug}/events/{eventSlug}/tickets/{result.Id}", result)
            : Results.NotFound();
    }

    private static Task<IResult> HandleUpdateEventTicket(string orgSlug, string eventSlug, Guid eventTicketId) => throw new NotImplementedException();
    private static Task<IResult> HandleDeleteEventTicket(string orgSlug, string eventSlug, Guid eventTicketId) => throw new NotImplementedException();
}
