using TimbnTicketing.Api.Auth;
using TimbnTicketing.Core;

namespace TimbnTicketing.Api.Endpoints;

public static class VenueEndpoints
{
    public static RouteGroupBuilder MapVenueEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", HandleListVenues);
        group.MapPost("/", HandleCreateVenue)
            .RequirePermission(Permission.CanManageEvents);
        group.MapPatch("/{venueId:guid}", HandleUpdateVenue)
            .RequirePermission(Permission.CanManageEvents);
        group.MapDelete("/{venueId:guid}", HandleDeleteVenue)
            .RequirePermission(Permission.CanManageEvents);

        return group;
    }

    private static Task<IResult> HandleListVenues(string orgSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleCreateVenue(string orgSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleUpdateVenue(string orgSlug, Guid venueId) => throw new NotImplementedException();
    private static Task<IResult> HandleDeleteVenue(string orgSlug, Guid venueId) => throw new NotImplementedException();
}
