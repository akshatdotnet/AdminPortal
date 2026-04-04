using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Events;

public record OrderConfirmedEvent(Guid OrderId, Guid CustomerId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
