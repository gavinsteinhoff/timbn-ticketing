using Stripe;
using Stripe.Checkout;
using TimbnTicketing.Core.Interfaces;

namespace TimbnTicketing.Infrastructure.Services;

public class StripeCheckoutService : IStripeCheckoutService
{
    public async Task<StripeCheckoutResult> CreateCheckoutSessionAsync(
        string connectedAccountId,
        Guid orderId,
        IReadOnlyList<StripeCheckoutLineItem> lineItems,
        int platformFeeCents,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default)
    {
        var requestOptions = new RequestOptions { StripeAccount = connectedAccountId };

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(new SessionCreateOptions
        {
            Mode = "payment",
            LineItems = lineItems.Select(li => new SessionLineItemOptions
            {
                Price = li.StripePriceId,
                Quantity = li.Quantity,
            }).ToList(),
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                ApplicationFeeAmount = platformFeeCents,
                Metadata = new Dictionary<string, string>
                {
                    ["order_id"] = orderId.ToString(),
                },
            },
            Metadata = new Dictionary<string, string>
            {
                ["order_id"] = orderId.ToString(),
            },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
        }, requestOptions, cancellationToken);

        var expiresAt = new DateTimeOffset(session.ExpiresAt, TimeSpan.Zero);

        return new StripeCheckoutResult(session.Id, session.Url, expiresAt);
    }
}
