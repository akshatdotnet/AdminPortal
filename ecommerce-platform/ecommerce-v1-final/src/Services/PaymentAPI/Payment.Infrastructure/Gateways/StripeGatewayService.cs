using Common.Domain.Primitives;
using Microsoft.Extensions.Configuration;
using Payment.Application.Interfaces;

namespace Payment.Infrastructure.Gateways;

/// <summary>
/// Mock Stripe gateway — works with NO real Stripe keys in development.
/// Returns fake session IDs so you can test the full order flow locally.
/// Replace with real Stripe.net SDK calls for production.
/// </summary>
public sealed class StripeGatewayService(IConfiguration config) : IPaymentGatewayService
{
    public async Task<Result<GatewaySession>> CreateSessionAsync(
        CreateSessionRequest request, CancellationToken ct)
    {
        // MOCK: Generate fake session data for local testing
        // Production: replace with Stripe.net SessionService.CreateAsync(...)
        var mockSessionId      = "cs_test_" + Guid.NewGuid().ToString("N")[..24];
        var mockPaymentIntentId = "pi_test_" + Guid.NewGuid().ToString("N")[..24];
        var mockCheckoutUrl    = $"https://checkout.stripe.com/pay/{mockSessionId}";

        await Task.CompletedTask;
        return Result.Success(new GatewaySession(mockSessionId, mockPaymentIntentId, mockCheckoutUrl));
    }

    public async Task<Result<WebhookEvent>> ParseWebhookAsync(
        string payload, string signature, CancellationToken ct)
    {
        // MOCK: Parse the simplified test payload sent by test-e2e scripts
        // Production: use Stripe.net EventUtility.ConstructEvent(payload, signature, webhookSecret)
        await Task.CompletedTask;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(payload);
            var root      = doc.RootElement;
            var eventType = root.GetProperty("type").GetString() ?? "payment.succeeded";
            var data      = root.GetProperty("data").GetProperty("object");
            var piId      = data.GetProperty("id").GetString() ?? string.Empty;
            return Result.Success(new WebhookEvent(eventType, piId));
        }
        catch (Exception ex)
        {
            return Result.Failure<WebhookEvent>(
                Error.BusinessRule("Webhook", $"Invalid payload: {ex.Message}"));
        }
    }

    public async Task<Result> RefundAsync(string paymentId, decimal amount, CancellationToken ct)
    {
        // MOCK: always succeeds locally
        // Production: use Stripe.net RefundService.CreateAsync(...)
        await Task.CompletedTask;
        return Result.Success();
    }
}

public sealed class PaymentGatewayFactory(IConfiguration config) : IPaymentGatewayFactory
{
    public IPaymentGatewayService Create(Payment.Domain.Entities.PaymentGateway gateway) =>
        new StripeGatewayService(config);
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
