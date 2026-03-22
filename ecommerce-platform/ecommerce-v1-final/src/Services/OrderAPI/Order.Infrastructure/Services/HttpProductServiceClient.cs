using Common.Domain.Primitives;
using Order.Application.Interfaces;
using System.Net.Http.Json;

namespace Order.Infrastructure.Services;

public sealed class HttpProductServiceClient(HttpClient http) : IProductServiceClient
{
    public async Task<Result> ReserveStockAsync(Guid productId, int quantity, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync(
            $"/api/v1/products/{productId}/stock",
            new { Delta = -quantity, Reason = "Order reservation" }, ct);

        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(Error.BusinessRule("StockReservation",
                $"Could not reserve {quantity} units of product {productId}."));
    }

    public async Task<Result> ReleaseStockAsync(Guid productId, int quantity, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync(
            $"/api/v1/products/{productId}/stock",
            new { Delta = quantity, Reason = "Order cancellation release" }, ct);

        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(Error.BusinessRule("StockRelease",
                $"Could not release {quantity} units of product {productId}."));
    }
}

public sealed class HttpCouponServiceClient(HttpClient http) : ICouponServiceClient
{
    public async Task<Result<CouponResult>> ValidateAsync(string code, decimal amount, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/api/v1/coupons/validate",
            new { Code = code, OrderAmount = amount }, ct);

        if (!response.IsSuccessStatusCode)
            return Result.Failure<CouponResult>(Error.BusinessRule("Coupon", "Invalid coupon code."));

        var result = await response.Content.ReadFromJsonAsync<CouponResult>(ct);
        return result is null
            ? Result.Failure<CouponResult>(Error.BusinessRule("Coupon", "Failed to parse coupon response."))
            : Result.Success(result);
    }
}
