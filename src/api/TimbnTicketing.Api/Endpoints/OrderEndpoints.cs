using TimbnTicketing.Api.Auth;
using TimbnTicketing.Core;

namespace TimbnTicketing.Api.Endpoints;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/checkout", HandleCheckout);
        group.MapPost("/webhook", HandleStripeWebhook)
            .AllowAnonymous();
        group.MapGet("/", HandleListOrders)
            .RequirePermission(Permission.CanViewAttendees);
        group.MapPost("/{orderId:guid}/refund", HandleRefundOrder)
            .RequirePermission(Permission.CanManageEvents);

        return group;
    }

    private static Task<IResult> HandleCheckout(string orgSlug, string eventSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleStripeWebhook(string orgSlug, string eventSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleListOrders(string orgSlug, string eventSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleRefundOrder(string orgSlug, string eventSlug, Guid orderId) => throw new NotImplementedException();
}
