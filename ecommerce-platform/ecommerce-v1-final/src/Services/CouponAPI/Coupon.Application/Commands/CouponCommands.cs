using Common.Domain.Primitives;
using Coupon.Application.DTOs;
using Coupon.Application.Interfaces;
using Coupon.Domain.Entities;
using FluentValidation;
using MediatR;

using CouponEntity = Coupon.Domain.Entities.Coupon;

namespace Coupon.Application.Commands;

// ---------------------------------------------------------------
// Validate Coupon
// ---------------------------------------------------------------
public sealed record ValidateCouponCommand(string Code, decimal OrderAmount)
    : IRequest<Result<CouponValidationResult>>;

public sealed class ValidateCouponHandler(ICouponRepository repo)
    : IRequestHandler<ValidateCouponCommand, Result<CouponValidationResult>>
{
    public async Task<Result<CouponValidationResult>> Handle(
        ValidateCouponCommand cmd, CancellationToken ct)
    {
        var coupon = await repo.GetByCodeAsync(cmd.Code, ct);
        if (coupon is null)
            return Result.Failure<CouponValidationResult>(
                Error.NotFound("Coupon", cmd.Code));

        var dr = coupon.CalculateDiscount(cmd.OrderAmount);
        if (!dr.IsSuccess)
            return Result.Failure<CouponValidationResult>(dr.Error);

        return Result.Success(new CouponValidationResult(
            coupon.Code, coupon.Description,
            dr.Value, coupon.DiscountType.ToString()));
    }
}

// ---------------------------------------------------------------
// Create Coupon
// ---------------------------------------------------------------
public sealed record CreateCouponCommand(
    string   Code,
    string   Description,
    string   DiscountType,
    decimal  DiscountValue,
    DateTime ValidFrom,
    DateTime ValidTo,
    decimal? MinimumOrderAmount,
    decimal? MaximumDiscountAmount,
    int?     MaxUsageCount)
    : IRequest<Result<Guid>>;

public sealed class CreateCouponValidator : AbstractValidator<CreateCouponCommand>
{
    public CreateCouponValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DiscountValue).GreaterThan(0);
        RuleFor(x => x.ValidTo)
            .GreaterThan(x => x.ValidFrom)
            .WithMessage("ValidTo must be after ValidFrom.");
        RuleFor(x => x.DiscountType)
            .Must(t => Enum.TryParse<DiscountType>(t, ignoreCase: true, out _))
            .WithMessage("DiscountType must be FixedAmount, Percentage, or FreeShipping.");
    }
}

public sealed class CreateCouponHandler(ICouponRepository repo, IUnitOfWorkCoupon uow)
    : IRequestHandler<CreateCouponCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCouponCommand cmd, CancellationToken ct)
    {
        if (await repo.CodeExistsAsync(cmd.Code, ct))
            return Result.Failure<Guid>(
                Error.Conflict("Coupon", $"Code '{cmd.Code}' already exists."));

        var discountType = Enum.Parse<DiscountType>(cmd.DiscountType, ignoreCase: true);

        // Use fully qualified name to avoid Coupon namespace collision
        var coupon = CouponEntity.Create(
            cmd.Code, cmd.Description, discountType,
            cmd.DiscountValue, cmd.ValidFrom, cmd.ValidTo,
            cmd.MinimumOrderAmount, cmd.MaximumDiscountAmount, cmd.MaxUsageCount);

        repo.Add(coupon);
        await uow.SaveChangesAsync(ct);
        return Result.Success(coupon.Id);
    }
}
