using Common.Domain.Entities;
using Coupon.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coupon.Infrastructure.Persistence;

public sealed class CouponDbContext(DbContextOptions<CouponDbContext> options, IMediator mediator)
    : DbContext(options)
{
    public DbSet<Coupon.Domain.Entities.Coupon> Coupons => Set<Coupon.Domain.Entities.Coupon>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(CouponDbContext).Assembly);
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

public sealed class CouponConfig : IEntityTypeConfiguration<Coupon.Domain.Entities.Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon.Domain.Entities.Coupon> b)
    {
        b.HasKey(c => c.Id);
        b.Property(c => c.Code).IsRequired().HasMaxLength(50);
        b.HasIndex(c => c.Code).IsUnique();
        b.Property(c => c.Description).IsRequired().HasMaxLength(500);
        b.Property(c => c.DiscountType).HasConversion<string>().HasMaxLength(20);
        b.Property(c => c.DiscountValue).HasColumnType("decimal(18,2)");
        b.Property(c => c.MinimumOrderAmount).HasColumnType("decimal(18,2)");
        b.Property(c => c.MaximumDiscountAmount).HasColumnType("decimal(18,2)");
        b.Ignore(c => c.DomainEvents);
    }
}
