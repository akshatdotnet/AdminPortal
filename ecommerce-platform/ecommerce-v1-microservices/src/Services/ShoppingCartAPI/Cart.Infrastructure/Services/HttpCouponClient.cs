using Cart.Application.Interfaces;
using Common.Domain.Primitives;
using System.Net.Http.Json;

namespace Cart.Infrastructure.Services;

public sealed class HttpCouponClient(HttpClient http) : ICouponServiceClient
{
    public async Task<Result<decimal>> ValidateAsync(
        string code, decimal amount, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/api/v1/coupons/validate",
            new { Code = code, OrderAmount = amount }, ct);
        if (!response.IsSuccessStatusCode)
            return Result.Failure<decimal>(Error.BusinessRule("Coupon", "Invalid or expired coupon."));
        var result = await response.Content.ReadFromJsonAsync<CouponResponse>(ct);
        return result is null
            ? Result.Failure<decimal>(Error.BusinessRule("Coupon", "Parse failed."))
            : Result.Success(result.DiscountAmount);
    }
    private sealed record CouponResponse(string Code, decimal DiscountAmount);
}
