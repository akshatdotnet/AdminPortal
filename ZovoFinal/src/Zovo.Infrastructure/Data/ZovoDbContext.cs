using Microsoft.EntityFrameworkCore;
using Zovo.Core.Entities;
using Zovo.Infrastructure.Data.Seeding;

namespace Zovo.Infrastructure.Data;

public class ZovoDbContext : DbContext
{
    public ZovoDbContext(DbContextOptions<ZovoDbContext> options) : base(options) { }

    public DbSet<Product>       Products      => Set<Product>();
    public DbSet<Customer>      Customers     => Set<Customer>();
    public DbSet<Address>       Addresses     => Set<Address>();
    public DbSet<Order>         Orders        => Set<Order>();
    public DbSet<OrderItem>     OrderItems    => Set<OrderItem>();
    public DbSet<StoreSettings> StoreSettings => Set<StoreSettings>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Product>(e => {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.SKU).IsUnique().HasFilter("[SKU] IS NOT NULL");
            e.HasIndex(p => p.Category);
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.SKU).HasMaxLength(50);
            e.Property(p => p.Slug).HasMaxLength(220);
            e.Property(p => p.Category).HasMaxLength(100).IsRequired();
            e.Property(p => p.Price).HasPrecision(18, 2);
            e.Property(p => p.CompareAtPrice).HasPrecision(18, 2);
            e.Property(p => p.CostPrice).HasPrecision(18, 2);
            e.Property(p => p.Weight).HasPrecision(8, 3);
        });

        mb.Entity<Customer>(e => {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Email).IsUnique();
            e.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
            e.Property(c => c.LastName).HasMaxLength(100).IsRequired();
            e.Property(c => c.Email).HasMaxLength(200).IsRequired();
            e.Ignore(c => c.FullName);
            e.Ignore(c => c.Initials);
            e.HasMany(c => c.Orders).WithOne(o => o.Customer).HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(c => c.Addresses).WithOne(a => a.Customer).HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Address>(e => {
            e.HasKey(a => a.Id);
            e.Property(a => a.Line1).HasMaxLength(200).IsRequired();
            e.Property(a => a.City).HasMaxLength(100).IsRequired();
            e.Property(a => a.State).HasMaxLength(100).IsRequired();
            e.Property(a => a.PostalCode).HasMaxLength(20).IsRequired();
        });

        mb.Entity<Order>(e => {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.Property(o => o.OrderNumber).HasMaxLength(20).IsRequired();
            e.Property(o => o.SubTotal).HasPrecision(18, 2);
            e.Property(o => o.ShippingCost).HasPrecision(18, 2);
            e.Property(o => o.TaxAmount).HasPrecision(18, 2);
            e.Property(o => o.TotalAmount).HasPrecision(18, 2);
            e.HasMany(o => o.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(o => o.ShippingAddress).WithMany().HasForeignKey(o => o.ShippingAddressId).OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<OrderItem>(e => {
            e.HasKey(i => i.Id);
            e.Property(i => i.ProductName).HasMaxLength(200).IsRequired();
            e.Property(i => i.UnitPrice).HasPrecision(18, 2);
            e.Property(i => i.Discount).HasPrecision(18, 2);
            e.Ignore(i => i.LineTotal);
            e.HasOne(i => i.Product).WithMany(p => p.OrderItems).HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<StoreSettings>(e => {
            e.HasKey(s => s.Id);
            e.Property(s => s.TaxRate).HasPrecision(5, 2);
            e.Property(s => s.FreeShippingThreshold).HasPrecision(18, 2);
            e.Property(s => s.DefaultShippingCost).HasPrecision(18, 2);
        });

        ZovoSeed.Apply(mb);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return await base.SaveChangesAsync(ct);
    }
}
