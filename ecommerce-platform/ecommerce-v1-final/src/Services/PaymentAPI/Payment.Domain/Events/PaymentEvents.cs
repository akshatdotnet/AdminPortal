using Common.Domain.Entities;

namespace Payment.Domain.Entities;

public sealed record PaymentInitiatedEvent(Guid PaymentId, Guid OrderId, decimal Amount) : BaseDomainEvent;
public sealed record PaymentSucceededEvent(Guid PaymentId, Guid OrderId, Guid CustomerId, decimal Amount) : BaseDomainEvent;
public sealed record PaymentFailedEvent(Guid PaymentId, Guid OrderId, string Reason) : BaseDomainEvent;
