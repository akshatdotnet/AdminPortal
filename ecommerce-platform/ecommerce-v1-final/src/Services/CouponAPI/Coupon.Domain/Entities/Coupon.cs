using Common.Domain.Entities;
using Common.Domain.Primitives;

namespace Coupon.Domain.Entities;

public sealed class Coupon : BaseEntity
{
    private Coupon() { }

    public string Code { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public DiscountType DiscountType { get; private set; }
    public decimal DiscountValue { get; private set; }
    public decimal? MinimumOrderAmount { get; private set; }
    public decimal? MaximumDiscountAmount { get; private set; }
    public int? MaxUsageCount { get; private set; }
    public int UsageCount { get; private set; }
    public DateTime ValidFrom { get; private set; }
    public DateTime ValidTo { get; private set; }
    public bool IsActive { get; private set; } = true;

    public bool IsValid =>
        IsActive && DateTime.UtcNow >= ValidFrom && DateTime.UtcNow <= ValidTo
        && (MaxUsageCount is null || UsageCount < MaxUsageCount);

    public static Coupon Create(
        string code, string description,
        DiscountType discountType, decimal discountValue,
        DateTime validFrom, DateTime validTo,
        decimal? minimumOrderAmount = null,
        decimal? maximumDiscountAmount = null,
        int? maxUsageCount = null)
    {
        if (discountType == DiscountType.Percentage && (discountValue <= 0 || discountValue > 100))
            throw new ArgumentException("Percentage must be 1-100.");

        return new Coupon
        {
            Code = code.ToUpperInvariant(), Description = description,
            DiscountType = discountType, DiscountValue = discountValue,
            ValidFrom = validFrom, ValidTo = validTo,
            MinimumOrderAmount = minimumOrderAmount,
            MaximumDiscountAmount = maximumDiscountAmount,
            MaxUsageCount = maxUsageCount
        };
    }

    public Result<decimal> CalculateDiscount(decimal orderAmount)
    {
        if (!IsValid)
            return Result.Failure<decimal>(
                Error.BusinessRule("Coupon", "Coupon is no longer valid."));

        if (MinimumOrderAmount.HasValue && orderAmount < MinimumOrderAmount.Value)
            return Result.Failure<decimal>(Error.BusinessRule("Coupon",
                $"Minimum order of {MinimumOrderAmount:C} required."));

        var discount = DiscountType switch
        {
            DiscountType.FixedAmount  => DiscountValue,
            DiscountType.Percentage   => Math.Round(orderAmount * DiscountValue / 100, 2),
            _ => throw new InvalidOperationException("Unknown discount type.")
        };

        if (MaximumDiscountAmount.HasValue)
            discount = Math.Min(discount, MaximumDiscountAmount.Value);

        return Result.Success(Math.Min(discount, orderAmount));
    }

    public void IncrementUsage() => UsageCount++;
    public void Deactivate() { IsActive = false; SetUpdated("admin"); }
}

public enum DiscountType { FixedAmount = 1, Percentage = 2, FreeShipping = 3 }
