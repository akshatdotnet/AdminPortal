using Common.Domain.Primitives;
using Order.Application.Interfaces;
using System.Net.Http.Json;

namespace Order.Infrastructure.Services;

public sealed class HttpProductServiceClient(HttpClient http) : IProductServiceClient
{
    public async Task<Result> ReserveStockAsync(
        Guid productId, int qty, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync(
            $"/api/v1/products/{productId}/stock",
            new { Delta = -qty, Reason = "Order reservation" }, ct);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(Error.BusinessRule("Stock",
                $"Could not reserve {qty} units of product {productId}."));
    }

    public async Task<Result> ReleaseStockAsync(
        Guid productId, int qty, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync(
            $"/api/v1/products/{productId}/stock",
            new { Delta = qty, Reason = "Order cancellation" }, ct);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(Error.BusinessRule("Stock", "Could not release stock."));
    }
}

public sealed class HttpCouponServiceClient(HttpClient http) : ICouponServiceClient
{
    public async Task<Result<decimal>> ValidateAsync(
        string code, decimal amount, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(
            "/api/v1/coupons/validate",
            new { Code = code, OrderAmount = amount }, ct);

        if (!response.IsSuccessStatusCode)
            return Result.Failure<decimal>(
                Error.BusinessRule("Coupon", "Invalid or expired coupon."));

        var result = await response.Content.ReadFromJsonAsync<CouponResponse>(ct);
        return result is null
            ? Result.Failure<decimal>(Error.BusinessRule("Coupon", "Parse failed."))
            : Result.Success(result.DiscountAmount);
    }

    private sealed record CouponResponse(string Code, decimal DiscountAmount);
}
