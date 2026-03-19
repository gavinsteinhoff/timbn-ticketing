using TimbnTicketing.Api.Auth;
using TimbnTicketing.Core;

namespace TimbnTicketing.Api.Endpoints;

public static class TicketTypeEndpoints
{
    public static RouteGroupBuilder MapTicketTypeEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", HandleListTicketTypes);
        group.MapPost("/", HandleCreateTicketType)
            .RequirePermission(Permission.CanManageEvents);
        group.MapPatch("/{ticketTypeId:guid}", HandleUpdateTicketType)
            .RequirePermission(Permission.CanManageEvents);

        return group;
    }

    private static Task<IResult> HandleListTicketTypes(string orgSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleCreateTicketType(string orgSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleUpdateTicketType(string orgSlug, Guid ticketTypeId) => throw new NotImplementedException();
}
