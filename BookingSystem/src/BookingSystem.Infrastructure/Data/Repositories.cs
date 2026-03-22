using BookingSystem.Core.Entities;
using BookingSystem.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Data;

// ─── GENERIC REPOSITORY ───────────────────────────────────────────────────────
public class Repository<T>(AppDbContext db) : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext Db = db;
    protected DbSet<T> Set => Db.Set<T>();

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await Set.FindAsync([id], ct);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default) =>
        await Set.ToListAsync(ct);

    public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await Set.AddAsync(entity, ct);
        return entity;
    }

    public virtual Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        Db.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct);
        if (entity is not null) Set.Remove(entity);
    }
}

// ─── BOOKING REPOSITORY ───────────────────────────────────────────────────────
public class BookingRepository(AppDbContext db)
    : Repository<Booking>(db), IBookingRepository
{
    public async Task<IReadOnlyList<Booking>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        await Db.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Venue)
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Booking>> GetByVenueAndDateAsync(Guid venueId, DateTime date, CancellationToken ct = default) =>
        await Db.Bookings
            .Where(b => b.VenueId == venueId &&
                        b.SlotDate.Date == date.Date &&
                        b.Status != BookingStatus.Cancelled)
            .ToListAsync(ct);

    public async Task<bool> IsSlotAvailableAsync(Guid venueId, DateTime slotDate, int guestCount, CancellationToken ct = default)
    {
        var venue = await Db.Venues.FindAsync([venueId], ct);
        if (venue is null) return false;

        var existingGuests = await Db.Bookings
            .Where(b => b.VenueId == venueId &&
                        b.SlotDate == slotDate &&
                        b.Status != BookingStatus.Cancelled)
            .SumAsync(b => b.GuestCount, ct);

        return (existingGuests + guestCount) <= venue.Capacity;
    }
}

// ─── ORDER REPOSITORY ─────────────────────────────────────────────────────────
public class OrderRepository(AppDbContext db)
    : Repository<Order>(db), IOrderRepository
{
    public async Task<Order?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default) =>
        await Db.Orders.FirstOrDefaultAsync(o => o.BookingId == bookingId, ct);

    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        await Db.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
}

// ─── CUSTOMER REPOSITORY ──────────────────────────────────────────────────────
public class CustomerRepository(AppDbContext db)
    : Repository<Customer>(db), ICustomerRepository
{
    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await Db.Customers.FirstOrDefaultAsync(c => c.Email == email, ct);
}

// ─── VENUE REPOSITORY ─────────────────────────────────────────────────────────
public class VenueRepository(AppDbContext db)
    : Repository<Venue>(db), IVenueRepository
{
    public async Task<IReadOnlyList<Venue>> GetByCityAsync(string city, CancellationToken ct = default) =>
        await Db.Venues.Where(v => v.City == city).ToListAsync(ct);
}

// ─── UNIT OF WORK ─────────────────────────────────────────────────────────────
public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public IBookingRepository Bookings { get; } = new BookingRepository(db);
    public IOrderRepository Orders { get; } = new OrderRepository(db);
    public ICustomerRepository Customers { get; } = new CustomerRepository(db);
    public IVenueRepository Venues { get; } = new VenueRepository(db);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
    public void Dispose() => db.Dispose();
}
