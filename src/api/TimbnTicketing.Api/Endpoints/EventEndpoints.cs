using TimbnTicketing.Api.Auth;
using TimbnTicketing.Core;

namespace TimbnTicketing.Api.Endpoints;

public static class EventEndpoints
{
    public static RouteGroupBuilder MapEventEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", HandleListEvents);
        group.MapGet("/{eventSlug}", HandleGetEvent);
        group.MapPost("/", HandleCreateEvent)
            .RequirePermission(Permission.CanCreateEvents);
        group.MapPatch("/{eventSlug}", HandleUpdateEvent)
            .RequirePermission(Permission.CanManageEvents);
        group.MapDelete("/{eventSlug}", HandleDeleteEvent)
            .RequirePermission(Permission.CanManageEvents);

        return group;
    }

    private static Task<IResult> HandleListEvents(string orgSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleGetEvent(string orgSlug, string eventSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleCreateEvent(string orgSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleUpdateEvent(string orgSlug, string eventSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleDeleteEvent(string orgSlug, string eventSlug) => throw new NotImplementedException();
}
