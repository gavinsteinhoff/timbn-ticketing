namespace TimbnTicketing.Core.Interfaces;

public record StripeProductResult(string ProductId, string PriceId);

public interface IStripeProductService
{
    Task<StripeProductResult> CreateProductAsync(
        string connectedAccountId,
        string productName,
        string? productDescription,
        int priceCents,
        CancellationToken cancellationToken = default);
}
