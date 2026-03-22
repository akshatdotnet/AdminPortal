using Common.Domain.Entities;

namespace Order.Domain.Entities;

public sealed record OrderPlacedEvent(Guid OrderId, string OrderNumber, Guid CustomerId) : BaseDomainEvent;
public sealed record OrderConfirmedEvent(Guid OrderId, string OrderNumber, Guid CustomerId, decimal Total) : BaseDomainEvent;
public sealed record OrderProcessingEvent(Guid OrderId, string OrderNumber) : BaseDomainEvent;
public sealed record OrderShippedEvent(Guid OrderId, string OrderNumber, Guid CustomerId, string TrackingNumber) : BaseDomainEvent;
public sealed record OrderDeliveredEvent(Guid OrderId, string OrderNumber, Guid CustomerId) : BaseDomainEvent;
public sealed record OrderCancelledEvent(Guid OrderId, string OrderNumber, Guid CustomerId, string Reason, bool RequiresRefund) : BaseDomainEvent;
