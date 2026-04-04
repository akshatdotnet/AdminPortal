using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<Entity> entities, CancellationToken ct = default);
}
