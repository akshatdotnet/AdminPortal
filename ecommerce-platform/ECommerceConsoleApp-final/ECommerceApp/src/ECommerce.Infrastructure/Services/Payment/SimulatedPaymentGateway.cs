using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services.Payment;

/// <summary>
/// Simulated payment gateway for local development.
/// COD always succeeds. Card/UPI succeed 90% of the time to simulate realistic failures.
/// Replace with real Stripe/Razorpay SDK in production.
/// </summary>
public class SimulatedPaymentGateway(ILogger<SimulatedPaymentGateway> logger) : IPaymentGateway
{
    private static readonly Random _rng = new();

    public async Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct = default)
    {
        await Task.Delay(500, ct); // Simulate network latency

        // COD always succeeds
        if (request.Method == PaymentMethod.CashOnDelivery)
        {
            logger.LogInformation("[Gateway] COD payment accepted for Order {OrderId}", request.OrderId);
            return new PaymentResult(true, $"COD-{Guid.NewGuid().ToString()[..8].ToUpper()}", null);
        }

        // Simulate 90% success rate for other methods
        bool success = _rng.NextDouble() > 0.10;
        if (success)
        {
            var txnId = $"TXN-{Guid.NewGuid().ToString()[..12].ToUpper()}";
            logger.LogInformation("[Gateway] Payment SUCCESS for Order {OrderId}. TxnId: {TxnId}", request.OrderId, txnId);
            return new PaymentResult(true, txnId, null);
        }

        logger.LogWarning("[Gateway] Payment FAILED for Order {OrderId}", request.OrderId);
        return new PaymentResult(false, null, "Insufficient funds or card declined.");
    }

    public async Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken ct = default)
    {
        await Task.Delay(300, ct);
        var refundId = $"REF-{Guid.NewGuid().ToString()[..10].ToUpper()}";
        logger.LogInformation("[Gateway] Refund initiated for Order {OrderId}. RefundId: {RefundId}", request.OrderId, refundId);
        return new RefundResult(true, refundId, null);
    }
}
