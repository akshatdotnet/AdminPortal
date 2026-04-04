using Common.Domain.Entities;

namespace Product.Domain.Entities;

public sealed record ProductCreatedEvent(
    Guid ProductId, string Name, string Sku) : BaseDomainEvent;

public sealed record ProductLowStockEvent(
    Guid ProductId, string Name, int CurrentStock) : BaseDomainEvent;

public sealed record ProductOutOfStockEvent(
    Guid ProductId, string Name) : BaseDomainEvent;
