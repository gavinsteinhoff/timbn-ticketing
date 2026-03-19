using TimbnTicketing.Api.Auth;
using TimbnTicketing.Core;

namespace TimbnTicketing.Api.Endpoints;

public static class EventTicketEndpoints
{
    public static RouteGroupBuilder MapEventTicketEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", HandleListEventTickets);
        group.MapPost("/", HandleCreateEventTicket)
            .RequirePermission(Permission.CanManageEvents);
        group.MapPatch("/{eventTicketId:guid}", HandleUpdateEventTicket)
            .RequirePermission(Permission.CanManageEvents);
        group.MapDelete("/{eventTicketId:guid}", HandleDeleteEventTicket)
            .RequirePermission(Permission.CanManageEvents);

        return group;
    }

    private static Task<IResult> HandleListEventTickets(string orgSlug, string eventSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleCreateEventTicket(string orgSlug, string eventSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleUpdateEventTicket(string orgSlug, string eventSlug, Guid eventTicketId) => throw new NotImplementedException();
    private static Task<IResult> HandleDeleteEventTicket(string orgSlug, string eventSlug, Guid eventTicketId) => throw new NotImplementedException();
}
