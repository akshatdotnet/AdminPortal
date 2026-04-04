namespace Coupon.Application.DTOs;

public sealed record CouponValidationResult(
    string Code, string Description,
    decimal DiscountAmount, string DiscountType);
