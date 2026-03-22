using BookingSystem.Core.Entities;

namespace BookingSystem.Core.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IBookingRepository : IRepository<Booking>
{
    Task<IReadOnlyList<Booking>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<Booking>> GetByVenueAndDateAsync(Guid venueId, DateTime date, CancellationToken ct = default);
    Task<bool> IsSlotAvailableAsync(Guid venueId, DateTime slotDate, int guestCount, CancellationToken ct = default);
}

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
}

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
}

public interface IVenueRepository : IRepository<Venue>
{
    Task<IReadOnlyList<Venue>> GetByCityAsync(string city, CancellationToken ct = default);
}

public interface IUnitOfWork : IDisposable
{
    IBookingRepository Bookings { get; }
    IOrderRepository Orders { get; }
    ICustomerRepository Customers { get; }
    IVenueRepository Venues { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

// Cache abstraction
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class;
    Task RemoveAsync(string key, CancellationToken ct = default);
}

// Event bus abstraction
public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class;
}
