using Stripe;
using TimbnTicketing.Core.Interfaces;

namespace TimbnTicketing.Infrastructure.Services;

public class StripeProductService : IStripeProductService
{
    public async Task<StripeProductResult> CreateProductAsync(
        string connectedAccountId,
        string productName,
        string? productDescription,
        int priceCents,
        CancellationToken cancellationToken = default)
    {
        var requestOptions = new RequestOptions { StripeAccount = connectedAccountId };

        var productService = new ProductService();
        var product = await productService.CreateAsync(new ProductCreateOptions
        {
            Name = productName,
            Description = productDescription,
        }, requestOptions, cancellationToken);

        var priceService = new PriceService();
        var price = await priceService.CreateAsync(new PriceCreateOptions
        {
            Product = product.Id,
            UnitAmount = priceCents,
            Currency = "usd",
        }, requestOptions, cancellationToken);

        return new StripeProductResult(product.Id, price.Id);
    }
}
