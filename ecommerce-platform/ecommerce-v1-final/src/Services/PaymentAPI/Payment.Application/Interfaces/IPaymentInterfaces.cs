using Common.Domain.Primitives;
using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IPaymentRepository
{
    Task<PaymentRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PaymentRecord?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<PaymentRecord?> GetByGatewayIdAsync(string paymentId, CancellationToken ct = default);
    void Add(PaymentRecord record);
    void Update(PaymentRecord record);
}

public interface IUnitOfWorkPayment
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IPaymentGatewayService
{
    Task<Result<GatewaySession>> CreateSessionAsync(
        Guid orderId, decimal amount, string currency,
        string successUrl, string cancelUrl, CancellationToken ct);

    Task<Result> RefundAsync(string paymentId, decimal amount, CancellationToken ct);
}

public interface IOrderServiceClient
{
    Task ConfirmPaymentAsync(Guid orderId, string paymentIntentId, CancellationToken ct);
}

public sealed record GatewaySession(string SessionId, string PaymentIntentId, string CheckoutUrl);
