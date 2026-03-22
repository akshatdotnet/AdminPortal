using BookingSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Customer
        mb.Entity<Customer>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Email).IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.Email).IsUnique();
        });

        // Venue
        mb.Entity<Venue>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
        });

        // Booking
        mb.Entity<Booking>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
            e.HasOne(x => x.Venue).WithMany().HasForeignKey(x => x.VenueId);
            e.HasIndex(x => new { x.VenueId, x.SlotDate });
        });

        // Order
        mb.Entity<Order>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Booking).WithOne(b => b.Order)
                .HasForeignKey<Order>(x => x.BookingId);
        });

        // OrderItem
        mb.Entity<OrderItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
        });

        // Seed data for local development
        var customer1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var customer2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var venue1Id    = Guid.Parse("00000000-0000-0000-0000-000000000011");
        var venue2Id    = Guid.Parse("00000000-0000-0000-0000-000000000012");

        mb.Entity<Customer>().HasData(
            new { Id = customer1Id, Name = "Rahul Sharma", Email = "rahul@example.com", Phone = "9876543210", CreatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = (DateTime?)null },
            new { Id = customer2Id, Name = "Priya Patel",  Email = "priya@example.com", Phone = "9876543211", CreatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = (DateTime?)null }
        );

        mb.Entity<Venue>().HasData(
            new { Id = venue1Id, Name = "Grand Ballroom Mumbai",  City = "Mumbai", Capacity = 300, CreatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = (DateTime?)null },
            new { Id = venue2Id, Name = "Sunset Terrace Pune",    City = "Pune",   Capacity = 150, CreatedAt = new DateTime(2024,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = (DateTime?)null }
        );
    }
}
