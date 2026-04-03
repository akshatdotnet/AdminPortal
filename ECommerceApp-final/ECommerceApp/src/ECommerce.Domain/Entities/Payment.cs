using ECommerce.Domain.Enums;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

/// <summary>Payment Aggregate Root. Records payment attempts and their outcomes.</summary>
public class Payment : Entity
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Money Amount { get; private set; } = Money.Zero;
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? GatewayTransactionId { get; private set; }
    public string? FailureReason { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private Payment() { }

    public static Payment Create(Guid orderId, Money amount, PaymentMethod method)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = amount,
            Method = method,
            Status = PaymentStatus.Pending,
            IdempotencyKey = $"{orderId}:{Guid.NewGuid()}",
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkCaptured(string gatewayTransactionId)
    {
        Status = PaymentStatus.Captured;
        GatewayTransactionId = gatewayTransactionId;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = PaymentStatus.Failed;
        FailureReason = reason;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkRefunded()
    {
        if (Status != PaymentStatus.Captured)
            throw new DomainException("Only captured payments can be refunded.");
        Status = PaymentStatus.Refunded;
        ProcessedAt = DateTime.UtcNow;
    }
}
