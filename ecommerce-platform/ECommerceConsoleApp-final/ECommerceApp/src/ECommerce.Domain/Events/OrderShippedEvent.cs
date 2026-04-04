using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Events;

public record OrderShippedEvent(Guid OrderId, Guid CustomerId, string TrackingNumber) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
