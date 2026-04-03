using ECommerce.Domain.Entities;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Events;

public record OrderCancelledEvent(Guid OrderId, Guid CustomerId, Money TotalAmount, string Reason) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
