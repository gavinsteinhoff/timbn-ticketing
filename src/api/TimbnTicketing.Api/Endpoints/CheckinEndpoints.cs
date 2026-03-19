using TimbnTicketing.Api.Auth;
using TimbnTicketing.Core;

namespace TimbnTicketing.Api.Endpoints;

public static class CheckinEndpoints
{
    public static RouteGroupBuilder MapCheckinEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", HandleCheckin)
            .RequirePermission(Permission.CanCheckin);
        group.MapGet("/stats", HandleGetCheckinStats)
            .RequirePermission(Permission.CanCheckin);

        return group;
    }

    private static Task<IResult> HandleCheckin(string orgSlug, string eventSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleGetCheckinStats(string orgSlug, string eventSlug) => throw new NotImplementedException();
}
