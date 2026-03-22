using Common.Domain.Entities;

namespace Payment.Domain.Entities;

public sealed class PaymentRecord : BaseEntity
{
    private PaymentRecord() { }

    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public PaymentGateway Gateway { get; private set; }
    public string? GatewayPaymentId { get; private set; }
    public string? GatewaySessionId { get; private set; }
    public string? CheckoutUrl { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public decimal? RefundAmount { get; private set; }

    public static PaymentRecord Create(Guid orderId, Guid customerId,
        decimal amount, string currency, PaymentGateway gateway)
    {
        var p = new PaymentRecord
        {
            OrderId = orderId, CustomerId = customerId,
            Amount = amount, Currency = currency, Gateway = gateway
        };
        p.AddDomainEvent(new PaymentInitiatedEvent(p.Id, orderId, amount));
        return p;
    }

    public void SetSession(string sessionId, string paymentId, string checkoutUrl)
    {
        GatewaySessionId = sessionId;
        GatewayPaymentId = paymentId;
        CheckoutUrl = checkoutUrl;
    }

    public void MarkSucceeded(string paymentId)
    {
        Status = PaymentStatus.Succeeded;
        GatewayPaymentId = paymentId;
        ProcessedAt = DateTime.UtcNow;
        AddDomainEvent(new PaymentSucceededEvent(Id, OrderId, CustomerId, Amount));
    }

    public void MarkFailed(string reason)
    {
        Status = PaymentStatus.Failed;
        FailureReason = reason;
        ProcessedAt = DateTime.UtcNow;
    }

    public void InitiateRefund(decimal amount)
    {
        if (Status != PaymentStatus.Succeeded)
            throw new InvalidOperationException("Can only refund succeeded payments.");
        RefundAmount = amount;
        Status = PaymentStatus.RefundInitiated;
    }

    public void MarkRefunded()
    {
        Status = PaymentStatus.Refunded;
        ProcessedAt = DateTime.UtcNow;
    }
}

public enum PaymentStatus { Pending, Succeeded, Failed, RefundInitiated, Refunded }
public enum PaymentGateway { Stripe, PayPal, Razorpay }
