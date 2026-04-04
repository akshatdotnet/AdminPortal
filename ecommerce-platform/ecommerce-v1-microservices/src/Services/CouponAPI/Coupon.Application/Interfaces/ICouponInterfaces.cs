using Coupon.Domain.Entities;

using CouponEntity = Coupon.Domain.Entities.Coupon;

namespace Coupon.Application.Interfaces;

public interface ICouponRepository
{
    Task<CouponEntity?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<bool>          CodeExistsAsync(string code, CancellationToken ct = default);
    void Add(CouponEntity coupon);
}

public interface IUnitOfWorkCoupon
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
