using Coupon.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

using CouponEntity = Coupon.Domain.Entities.Coupon;

namespace Coupon.Infrastructure.Persistence;

public sealed class CouponRepository(CouponDbContext ctx) : ICouponRepository
{
    public async Task<CouponEntity?> GetByCodeAsync(
        string code, CancellationToken ct = default)
        => await ctx.Coupons
            .FirstOrDefaultAsync(c => c.Code == code.ToUpperInvariant(), ct);

    public async Task<bool> CodeExistsAsync(
        string code, CancellationToken ct = default)
        => await ctx.Coupons
            .AnyAsync(c => c.Code == code.ToUpperInvariant(), ct);

    public void Add(CouponEntity coupon) => ctx.Coupons.Add(coupon);
}

public sealed class UnitOfWorkCoupon(CouponDbContext ctx) : IUnitOfWorkCoupon
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => ctx.SaveChangesAsync(ct);
}
