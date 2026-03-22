using Common.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence;

public sealed class PaymentDbContext(
    DbContextOptions<PaymentDbContext> options, IMediator mediator)
    : DbContext(options)
{
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);
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

public sealed class PaymentConfig : IEntityTypeConfiguration<PaymentRecord>
{
    public void Configure(EntityTypeBuilder<PaymentRecord> b)
    {
        b.HasKey(p => p.Id);
        b.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        b.Property(p => p.RefundAmount).HasColumnType("decimal(18,2)");
        b.Property(p => p.Currency).IsRequired().HasMaxLength(3);
        b.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(p => p.Gateway).HasConversion<string>().HasMaxLength(20);
        b.Property(p => p.GatewayPaymentId).HasMaxLength(200);
        b.Property(p => p.GatewaySessionId).HasMaxLength(200);
        b.Property(p => p.CheckoutUrl).HasMaxLength(1000);
        b.Ignore(p => p.DomainEvents);
    }
}
