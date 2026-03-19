using TimbnTicketing.Api.Auth;
using TimbnTicketing.Api.Dtos.Responses;
using TimbnTicketing.Api.Services;

namespace TimbnTicketing.Api.Endpoints;

public static class CurrentUserEndpoints
{
    public static RouteGroupBuilder MapCurrentUserEndpoints(this RouteGroupBuilder group)
    {
        group.WithTags("Current User");

        group.MapGet("/", HandleGetCurrentUser)
            .WithName("GetCurrentUser")
            .WithSummary("Get the authenticated user's profile and org memberships")
            .Produces<CurrentUserResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/", HandleUpdateCurrentUser);
        group.MapGet("/tickets", HandleGetMyTickets);
        group.MapGet("/gifted-tickets", HandleGetMyGiftedTickets);
        group.MapGet("/orders", HandleGetMyOrders);

        return group;
    }

    private static async Task<IResult> HandleGetCurrentUser(
        CurrentUserContext userContext,
        CurrentUserService currentUserService)
    {
        var user = await currentUserService.GetByIdAsync(userContext.UserId);

        return user is not null
            ? Results.Ok(user)
            : Results.NotFound();
    }

    private static Task<IResult> HandleUpdateCurrentUser() => throw new NotImplementedException();
    private static Task<IResult> HandleGetMyTickets() => throw new NotImplementedException();
    private static Task<IResult> HandleGetMyGiftedTickets() => throw new NotImplementedException();
    private static Task<IResult> HandleGetMyOrders() => throw new NotImplementedException();
}
