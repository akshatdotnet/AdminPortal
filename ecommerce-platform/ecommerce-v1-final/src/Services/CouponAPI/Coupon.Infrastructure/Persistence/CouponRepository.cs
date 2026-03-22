using Coupon.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Coupon.Infrastructure.Persistence;

public sealed class CouponRepository(CouponDbContext ctx) : ICouponRepository
{
    public async Task<Coupon.Domain.Entities.Coupon?> GetByCodeAsync(
        string code, CancellationToken ct = default)
        => await ctx.Coupons
            .FirstOrDefaultAsync(c => c.Code == code.ToUpperInvariant(), ct);

    public async Task<bool> CodeExistsAsync(string code, CancellationToken ct = default)
        => await ctx.Coupons
            .AnyAsync(c => c.Code == code.ToUpperInvariant(), ct);

    public void Add(Coupon.Domain.Entities.Coupon coupon) => ctx.Coupons.Add(coupon);
}

public sealed class UnitOfWorkCoupon(CouponDbContext ctx) : IUnitOfWorkCoupon
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        ctx.SaveChangesAsync(ct);
}
