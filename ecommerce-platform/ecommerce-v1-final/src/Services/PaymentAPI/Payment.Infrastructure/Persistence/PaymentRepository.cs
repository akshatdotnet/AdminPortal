using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence;

public sealed class PaymentRepository(PaymentDbContext ctx) : IPaymentRepository
{
    public async Task<PaymentRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await ctx.Payments.FindAsync(new object[] { id }, ct);

    public async Task<PaymentRecord?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
        => await ctx.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

    public async Task<PaymentRecord?> GetByGatewayIdAsync(string paymentId, CancellationToken ct = default)
        => await ctx.Payments
            .FirstOrDefaultAsync(p => p.GatewayPaymentId == paymentId, ct);

    public void Add(PaymentRecord r)    => ctx.Payments.Add(r);
    public void Update(PaymentRecord r) => ctx.Payments.Update(r);
}

public sealed class UnitOfWorkPayment(PaymentDbContext ctx) : IUnitOfWorkPayment
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        ctx.SaveChangesAsync(ct);
}
