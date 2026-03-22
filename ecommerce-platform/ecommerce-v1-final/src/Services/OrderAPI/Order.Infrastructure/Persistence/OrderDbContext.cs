using Common.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.Entities;

namespace Order.Infrastructure.Persistence;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options, IMediator mediator)
    : DbContext(options)
{
    public DbSet<Order.Domain.Entities.Order> Orders => Set<Order.Domain.Entities.Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(OrderDbContext).Assembly);
        base.OnModelCreating(mb);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var entities = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any()).Select(e => e.Entity).ToList();
        var events = entities.SelectMany(e => e.DomainEvents).ToList();
        entities.ForEach(e => e.ClearDomainEvents());
        var result = await base.SaveChangesAsync(ct);
        foreach (var ev in events) await mediator.Publish(ev, ct);
        return result;
    }
}

public sealed class OrderConfig : IEntityTypeConfiguration<Order.Domain.Entities.Order>
{
    public void Configure(EntityTypeBuilder<Order.Domain.Entities.Order> b)
    {
        b.HasKey(o => o.Id);
        b.Property(o => o.OrderNumber).IsRequired().HasMaxLength(50);
        b.HasIndex(o => o.OrderNumber).IsUnique();
        b.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(o => o.PaymentStatus).HasConversion<string>().HasMaxLength(20);
        b.Property(o => o.Subtotal).HasColumnType("decimal(18,2)");
        b.Property(o => o.DiscountAmount).HasColumnType("decimal(18,2)");
        b.Property(o => o.ShippingCost).HasColumnType("decimal(18,2)");
        b.Property(o => o.TaxAmount).HasColumnType("decimal(18,2)");
        b.Property(o => o.Total).HasColumnType("decimal(18,2)");
        b.OwnsOne(o => o.ShippingAddress, sa =>
        {
            sa.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            sa.Property(x => x.Street).IsRequired().HasMaxLength(300);
            sa.Property(x => x.City).IsRequired().HasMaxLength(100);
            sa.Property(x => x.State).IsRequired().HasMaxLength(100);
            sa.Property(x => x.PostalCode).IsRequired().HasMaxLength(20);
            sa.Property(x => x.Country).IsRequired().HasMaxLength(100);
            sa.Property(x => x.Phone).IsRequired().HasMaxLength(25);
        });
        b.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasMany(o => o.StatusHistory).WithOne().HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Ignore(o => o.DomainEvents);
    }
}

public sealed class OrderItemConfig : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.HasKey(i => i.Id);
        b.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
        b.Property(i => i.Sku).IsRequired().HasMaxLength(50);
        b.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        b.Ignore(i => i.DomainEvents);
    }
}

public sealed class OrderHistoryConfig : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> b)
    {
        b.HasKey(h => h.Id);
        b.Property(h => h.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(h => h.Note).HasMaxLength(500);
        b.Ignore(h => h.DomainEvents);
    }
}
