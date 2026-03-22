using Common.Domain.Entities;
using Common.Domain.Primitives;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// ══════════════════════════════════════════════════════════════
// DOMAIN
// ══════════════════════════════════════════════════════════════
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
    public List<Guid> ApplicableProductIds { get; private set; } = [];
    public List<Guid> ApplicableCategoryIds { get; private set; } = [];

    public bool IsValid =>
        IsActive &&
        DateTime.UtcNow >= ValidFrom &&
        DateTime.UtcNow <= ValidTo &&
        (MaxUsageCount is null || UsageCount < MaxUsageCount);

    public static Coupon Create(
        string code, string description,
        DiscountType discountType, decimal discountValue,
        DateTime validFrom, DateTime validTo,
        decimal? minimumOrderAmount = null,
        decimal? maximumDiscountAmount = null,
        int? maxUsageCount = null)
    {
        if (discountType == DiscountType.Percentage && (discountValue <= 0 || discountValue > 100))
            throw new ArgumentException("Percentage discount must be between 1 and 100.");

        return new Coupon
        {
            Code = code.ToUpperInvariant(),
            Description = description,
            DiscountType = discountType,
            DiscountValue = discountValue,
            ValidFrom = validFrom,
            ValidTo = validTo,
            MinimumOrderAmount = minimumOrderAmount,
            MaximumDiscountAmount = maximumDiscountAmount,
            MaxUsageCount = maxUsageCount
        };
    }

    public Result<decimal> CalculateDiscount(decimal orderAmount)
    {
        if (!IsValid)
            return Result.Failure<decimal>(Error.BusinessRule("Coupon", "Coupon is no longer valid."));

        if (MinimumOrderAmount.HasValue && orderAmount < MinimumOrderAmount.Value)
            return Result.Failure<decimal>(Error.BusinessRule("Coupon",
                $"Minimum order amount of {MinimumOrderAmount:C} required."));

        var discount = DiscountType switch
        {
            DiscountType.FixedAmount => DiscountValue,
            DiscountType.Percentage => Math.Round(orderAmount * DiscountValue / 100, 2),
            _ => throw new InvalidOperationException("Unknown discount type.")
        };

        if (MaximumDiscountAmount.HasValue)
            discount = Math.Min(discount, MaximumDiscountAmount.Value);

        discount = Math.Min(discount, orderAmount); // Can't discount more than order total
        return Result.Success(discount);
    }

    public void IncrementUsage() => UsageCount++;
    public void Deactivate() { IsActive = false; SetUpdated("admin"); }
}

public enum DiscountType { FixedAmount = 1, Percentage = 2, FreeShipping = 3 }

// ══════════════════════════════════════════════════════════════
// APPLICATION
// ══════════════════════════════════════════════════════════════
namespace Coupon.Application.Commands;

public sealed record ValidateCouponCommand(string Code, decimal OrderAmount) :
    IRequest<Result<CouponValidationResult>>;

public sealed class ValidateCouponCommandHandler(ICouponRepository couponRepo) :
    IRequestHandler<ValidateCouponCommand, Result<CouponValidationResult>>
{
    public async Task<Result<CouponValidationResult>> Handle(
        ValidateCouponCommand cmd, CancellationToken ct)
    {
        var coupon = await couponRepo.GetByCodeAsync(cmd.Code, ct);
        if (coupon is null)
            return Result.Failure<CouponValidationResult>(
                Error.NotFound("Coupon", cmd.Code));

        var discountResult = coupon.CalculateDiscount(cmd.OrderAmount);
        if (!discountResult.IsSuccess)
            return Result.Failure<CouponValidationResult>(discountResult.Error);

        return Result.Success(new CouponValidationResult(
            coupon.Code,
            coupon.Description,
            discountResult.Value,
            coupon.DiscountType.ToString()));
    }
}

public sealed record CreateCouponCommand(
    string Code, string Description,
    string DiscountType, decimal DiscountValue,
    DateTime ValidFrom, DateTime ValidTo,
    decimal? MinimumOrderAmount, decimal? MaximumDiscountAmount,
    int? MaxUsageCount) : IRequest<Result<Guid>>;

public sealed class CreateCouponValidator : AbstractValidator<CreateCouponCommand>
{
    public CreateCouponValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50).Matches("^[A-Z0-9_-]+$")
            .WithMessage("Code must contain only uppercase letters, digits, hyphens, underscores.");
        RuleFor(x => x.DiscountValue).GreaterThan(0);
        RuleFor(x => x.ValidTo).GreaterThan(x => x.ValidFrom)
            .WithMessage("End date must be after start date.");
        RuleFor(x => x.MinimumOrderAmount).GreaterThanOrEqualTo(0).When(x => x.MinimumOrderAmount.HasValue);
    }
}

public sealed class CreateCouponCommandHandler(ICouponRepository repo, IUnitOfWorkCoupon uow) :
    IRequestHandler<CreateCouponCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCouponCommand cmd, CancellationToken ct)
    {
        if (await repo.CodeExistsAsync(cmd.Code, ct))
            return Result.Failure<Guid>(Error.Conflict("Coupon", $"Code '{cmd.Code}' already exists."));

        var discountType = Enum.Parse<Coupon.Domain.Entities.DiscountType>(cmd.DiscountType, ignoreCase: true);
        var coupon = Coupon.Domain.Entities.Coupon.Create(
            cmd.Code, cmd.Description, discountType, cmd.DiscountValue,
            cmd.ValidFrom, cmd.ValidTo,
            cmd.MinimumOrderAmount, cmd.MaximumDiscountAmount, cmd.MaxUsageCount);

        repo.Add(coupon);
        await uow.SaveChangesAsync(ct);
        return Result.Success(coupon.Id);
    }
}

public sealed record CouponValidationResult(
    string Code, string Description, decimal DiscountAmount, string DiscountType);

public interface ICouponRepository
{
    Task<Coupon.Domain.Entities.Coupon?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);
    void Add(Coupon.Domain.Entities.Coupon coupon);
}

public interface IUnitOfWorkCoupon
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

// ══════════════════════════════════════════════════════════════
// API CONTROLLER
// ══════════════════════════════════════════════════════════════
namespace Coupon.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public sealed class CouponsController(IMediator mediator) : ControllerBase
{
    /// <summary>Validate a coupon and compute discount amount.</summary>
    [HttpPost("validate")]
    [Authorize]
    [ProducesResponseType(typeof(CouponValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ValidateCoupon(
        [FromBody] ValidateCouponCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    /// <summary>Create a new coupon (Admin only).</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCoupon(
        [FromBody] CreateCouponCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(ValidateCoupon), new { id = result.Value }, result.Value)
            : Problem(result.Error);
    }

    private ObjectResult Problem(Error error) => Problem(
        detail: error.Message,
        statusCode: error.Code switch
        {
            var c when c.Contains("NotFound") => 404,
            var c when c.Contains("Conflict") => 409,
            var c when c.Contains("BusinessRule") => 422,
            _ => 400
        },
        title: error.Code);
}
