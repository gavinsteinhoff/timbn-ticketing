using TimbnTicketing.Api.Auth;
using TimbnTicketing.Api.Dtos.Requests;
using TimbnTicketing.Api.Dtos.Responses;
using TimbnTicketing.Api.Services;
using TimbnTicketing.Core;

namespace TimbnTicketing.Api.Endpoints;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this RouteGroupBuilder group)
    {
        group.WithTags("Orders");

        group.MapPost("/checkout", HandleCheckout)
            .WithName("Checkout")
            .WithSummary("Initiate a ticket purchase")
            .WithDescription("Validates ticket availability, dependencies, and discount codes, then creates a pending order and returns a Stripe Checkout URL.")
            .Accepts<CheckoutRequest>("application/json")
            .Produces<CheckoutResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/webhook", HandleStripeWebhook)
            .AllowAnonymous();

        group.MapGet("/", HandleListOrders)
            .RequirePermission(Permission.CanViewAttendees);

        group.MapPost("/{orderId:guid}/refund", HandleRefundOrder)
            .RequirePermission(Permission.CanManageEvents);

        return group;
    }

    private static async Task<IResult> HandleCheckout(
        string orgSlug,
        string eventSlug,
        CheckoutRequest request,
        OrderCheckoutService checkoutService,
        CurrentRequestContext requestContext,
        HttpRequest httpRequest,
        CancellationToken cancellationToken)
    {
        if (requestContext.OrgStripeConnectAccountId is null)
            return Results.Problem(
                detail: "This organization has not completed Stripe onboarding.",
                statusCode: StatusCodes.Status400BadRequest);

        var baseUrl = $"{httpRequest.Scheme}://{httpRequest.Host}";
        var successUrl = $"{baseUrl}/orgs/{orgSlug}/events/{eventSlug}/orders/checkout/success?session_id={{CHECKOUT_SESSION_ID}}";
        var cancelUrl = $"{baseUrl}/orgs/{orgSlug}/events/{eventSlug}/orders/checkout/cancelled";

        var result = await checkoutService.CreateCheckoutAsync(
            requestContext.OrganizationId!.Value,
            requestContext.EventId!.Value,
            requestContext.UserId,
            requestContext.OrgStripeConnectAccountId,
            request,
            successUrl,
            cancelUrl,
            cancellationToken);

        if (!result.IsSuccess)
        {
            var statusCode = result.ErrorCode switch
            {
                "EVENT_TICKET_NOT_FOUND" => StatusCodes.Status404NotFound,
                "SOLD_OUT" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest,
            };
            return Results.Problem(detail: result.ErrorMessage, statusCode: statusCode);
        }

        return Results.Created((string?)null, result.Response);
    }

    private static Task<IResult> HandleStripeWebhook(string orgSlug, string eventSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleListOrders(string orgSlug, string eventSlug) => throw new NotImplementedException();
    private static Task<IResult> HandleRefundOrder(string orgSlug, string eventSlug, Guid orderId) => throw new NotImplementedException();
}
