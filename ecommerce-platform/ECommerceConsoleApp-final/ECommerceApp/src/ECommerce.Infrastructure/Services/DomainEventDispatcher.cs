using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services;

/// <summary>
/// Dispatches domain events after persistence.
/// In production this would publish to a message bus (RabbitMQ / Azure Service Bus).
/// </summary>
public class DomainEventDispatcher(ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public Task DispatchEventsAsync(IEnumerable<Entity> entities, CancellationToken ct = default)
    {
        foreach (var entity in entities)
        {
            foreach (var evt in entity.DomainEvents)
            {
                logger.LogInformation("[DomainEvent] {EventType} raised at {OccurredOn}",
                    evt.GetType().Name, evt.OccurredOn);
            }
            entity.ClearDomainEvents();
        }
        return Task.CompletedTask;
    }
}
