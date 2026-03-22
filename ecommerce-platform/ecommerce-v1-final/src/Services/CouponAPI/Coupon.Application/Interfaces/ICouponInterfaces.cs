using Coupon.Domain.Entities;

namespace Coupon.Application.Interfaces;

public interface ICouponRepository
{
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);
    void Add(Coupon coupon);
}

public interface IUnitOfWorkCoupon
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
