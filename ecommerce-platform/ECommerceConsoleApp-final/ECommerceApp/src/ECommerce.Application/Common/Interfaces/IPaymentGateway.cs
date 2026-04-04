using ECommerce.Domain.Enums;

namespace ECommerce.Application.Common.Interfaces;

public record PaymentRequest(Guid OrderId, decimal Amount, string Currency, PaymentMethod Method, string IdempotencyKey);
public record PaymentResult(bool Success, string? TransactionId, string? FailureReason);
public record RefundRequest(Guid OrderId, string TransactionId, decimal Amount);
public record RefundResult(bool Success, string? RefundId, string? FailureReason);

public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct = default);
    Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken ct = default);
}
