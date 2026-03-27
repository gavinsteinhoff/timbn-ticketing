namespace TimbnTicketing.Core.Interfaces;

public record StripeCheckoutResult(string SessionId, string Url, DateTimeOffset ExpiresAt);

public record StripeCheckoutLineItem(string StripePriceId, int Quantity);

public interface IStripeCheckoutService
{
    Task<StripeCheckoutResult> CreateCheckoutSessionAsync(
        string connectedAccountId,
        Guid orderId,
        IReadOnlyList<StripeCheckoutLineItem> lineItems,
        int platformFeeCents,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default);
}
