using Common.Domain.Primitives;
using Payment.Application.Interfaces;

namespace Payment.Infrastructure.Services;

/// <summary>
/// Mock payment gateway — works with zero config in development.
/// Replace with Stripe.net SDK for production.
/// </summary>
public sealed class MockPaymentGateway : IPaymentGatewayService
{
    public async Task<Result<GatewaySession>> CreateSessionAsync(
        Guid orderId, decimal amount, string currency,
        string successUrl, string cancelUrl, CancellationToken ct)
    {
        await Task.CompletedTask;
        var sessionId   = "cs_test_"  + Guid.NewGuid().ToString("N")[..24];
        var paymentId   = "pi_test_"  + Guid.NewGuid().ToString("N")[..24];
        var checkoutUrl = $"https://checkout.stripe.com/pay/{sessionId}";
        return Result.Success(new GatewaySession(sessionId, paymentId, checkoutUrl));
    }

    public async Task<Result> RefundAsync(string paymentId, decimal amount, CancellationToken ct)
    {
        await Task.CompletedTask;
        return Result.Success();
    }
}

public sealed class HttpOrderServiceClient(HttpClient http) : IOrderServiceClient
{
    public async Task ConfirmPaymentAsync(Guid orderId, string paymentIntentId, CancellationToken ct)
    {
        await http.PostAsJsonAsync(
            $"/api/v1/orders/{orderId}/confirm-payment",
            new { PaymentIntentId = paymentIntentId }, ct);
    }
}
