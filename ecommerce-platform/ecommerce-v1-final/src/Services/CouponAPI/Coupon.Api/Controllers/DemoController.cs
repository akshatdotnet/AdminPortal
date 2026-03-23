using Asp.Versioning;
using Coupon.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coupon.Api.Controllers;

/// <summary>
/// Demo endpoint - runs the complete Coupon workflow in one HTTP call.
/// No auth required. POST /api/v1/demo/complete-flow
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/demo")]
[ApiVersion("1.0")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class DemoController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Runs the full Coupon service workflow (8 steps):
    ///  1. Create a fixed-amount coupon (SAVE20)
    ///  2. Create a percentage coupon (PERCENT15)
    ///  3. Validate SAVE20 against a qualifying order
    ///  4. Validate PERCENT15 against a qualifying order
    ///  5. Try to validate with order below minimum (must fail)
    ///  6. Try to validate a non-existent coupon (must fail 404)
    ///  7. Try to create a duplicate coupon code (must fail 409)
    ///  8. Validate percentage coupon caps at MaximumDiscountAmount
    /// </summary>
    [HttpPost("complete-flow")]
    [ProducesResponseType(typeof(CouponDemoResult), 200)]
    public async Task<IActionResult> RunCompleteFlow(CancellationToken ct)
    {
        var ts     = DateTime.UtcNow.Ticks;
        var result = new CouponDemoResult();

        var code1 = $"SAVE20-{ts % 100000}";
        var code2 = $"PCT15-{ts  % 100000}";

        // Step 1: Create fixed-amount coupon ($20 off, min order $50)
        var s1 = await mediator.Send(new CreateCouponCommand(
            Code:                  code1,
            Description:           "$20 off orders over $50",
            DiscountType:          "FixedAmount",
            DiscountValue:         20m,
            ValidFrom:             DateTime.UtcNow.AddMinutes(-1),
            ValidTo:               DateTime.UtcNow.AddDays(30),
            MinimumOrderAmount:    50m,
            MaximumDiscountAmount: null,
            MaxUsageCount:         100), ct);

        result.Step1_CreateFixedCoupon = Step(
            s1.IsSuccess,
            s1.IsSuccess
                ? $"Created '{code1}': $20 off orders over $50"
                : s1.Error.Message,
            s1.IsSuccess ? new { CouponId = s1.Value, Code = code1 } : null);

        // Step 2: Create percentage coupon (15% off, max $30 discount)
        var s2 = await mediator.Send(new CreateCouponCommand(
            Code:                  code2,
            Description:           "15% off, max $30 discount",
            DiscountType:          "Percentage",
            DiscountValue:         15m,
            ValidFrom:             DateTime.UtcNow.AddMinutes(-1),
            ValidTo:               DateTime.UtcNow.AddDays(30),
            MinimumOrderAmount:    null,
            MaximumDiscountAmount: 30m,
            MaxUsageCount:         null), ct);

        result.Step2_CreatePercentCoupon = Step(
            s2.IsSuccess,
            s2.IsSuccess
                ? $"Created '{code2}': 15% off, capped at $30"
                : s2.Error.Message,
            s2.IsSuccess ? new { CouponId = s2.Value, Code = code2 } : null);

        // Step 3: Validate fixed coupon against $100 order (expect $20 off)
        var s3 = await mediator.Send(
            new ValidateCouponCommand(code1, 100m), ct);

        result.Step3_ValidateFixed = Step(
            s3.IsSuccess && s3.Value.DiscountAmount == 20m,
            s3.IsSuccess
                ? $"Order $100 - discount: ${s3.Value.DiscountAmount} (expected $20)"
                : s3.Error.Message,
            s3.IsSuccess ? new { s3.Value.Code, s3.Value.DiscountAmount, s3.Value.DiscountType } : null);

        // Step 4: Validate percentage coupon against $100 order (expect $15)
        var s4 = await mediator.Send(
            new ValidateCouponCommand(code2, 100m), ct);

        result.Step4_ValidatePercent = Step(
            s4.IsSuccess && s4.Value.DiscountAmount == 15m,
            s4.IsSuccess
                ? $"Order $100 - 15% = ${s4.Value.DiscountAmount} (expected $15)"
                : s4.Error.Message,
            s4.IsSuccess ? new { s4.Value.Code, s4.Value.DiscountAmount } : null);

        // Step 5: Validate fixed coupon below minimum order ($30 < $50 minimum, must fail)
        var s5 = await mediator.Send(
            new ValidateCouponCommand(code1, 30m), ct);

        result.Step5_BelowMinimum = Step(
            !s5.IsSuccess,
            !s5.IsSuccess
                ? $"Correctly rejected: {s5.Error.Message}"
                : "BUG: coupon applied below minimum order",
            null);

        // Step 6: Validate non-existent coupon (must fail 404)
        var s6 = await mediator.Send(
            new ValidateCouponCommand("NOTREAL999", 100m), ct);

        result.Step6_InvalidCode = Step(
            !s6.IsSuccess,
            !s6.IsSuccess
                ? $"Correctly rejected: {s6.Error.Message}"
                : "BUG: non-existent coupon was accepted",
            null);

        // Step 7: Duplicate coupon code (must fail 409)
        var s7 = await mediator.Send(new CreateCouponCommand(
            Code:                  code1,
            Description:           "Duplicate attempt",
            DiscountType:          "FixedAmount",
            DiscountValue:         5m,
            ValidFrom:             DateTime.UtcNow,
            ValidTo:               DateTime.UtcNow.AddDays(1),
            MinimumOrderAmount:    null,
            MaximumDiscountAmount: null,
            MaxUsageCount:         null), ct);

        result.Step7_DuplicateCode = Step(
            !s7.IsSuccess,
            !s7.IsSuccess
                ? $"Correctly rejected duplicate: {s7.Error.Message}"
                : "BUG: duplicate code was accepted",
            null);

        // Step 8: Percentage coupon cap - $300 order, 15% = $45, but capped at $30
        var s8 = await mediator.Send(
            new ValidateCouponCommand(code2, 300m), ct);

        result.Step8_PercentCap = Step(
            s8.IsSuccess && s8.Value.DiscountAmount == 30m,
            s8.IsSuccess
                ? $"Order $300 - 15% = $45 but capped at $30. Got: ${s8.Value.DiscountAmount}"
                : s8.Error.Message,
            s8.IsSuccess ? new { s8.Value.DiscountAmount, ExpectedCap = 30m } : null);

        // Summary
        var steps = new[]
        {
            result.Step1_CreateFixedCoupon, result.Step2_CreatePercentCoupon,
            result.Step3_ValidateFixed,     result.Step4_ValidatePercent,
            result.Step5_BelowMinimum,      result.Step6_InvalidCode,
            result.Step7_DuplicateCode,     result.Step8_PercentCap
        };
        result.TotalSteps  = steps.Length;
        result.PassedSteps = steps.Count(s => s?.Success == true);
        result.AllPassed   = result.PassedSteps == result.TotalSteps;

        return Ok(result);
    }

    private static StepResult Step(bool ok, string msg, object? data) =>
        new() { Success = ok, Message = msg, Data = data };
}

public sealed class CouponDemoResult
{
    public StepResult? Step1_CreateFixedCoupon   { get; set; }
    public StepResult? Step2_CreatePercentCoupon { get; set; }
    public StepResult? Step3_ValidateFixed       { get; set; }
    public StepResult? Step4_ValidatePercent     { get; set; }
    public StepResult? Step5_BelowMinimum        { get; set; }
    public StepResult? Step6_InvalidCode         { get; set; }
    public StepResult? Step7_DuplicateCode       { get; set; }
    public StepResult? Step8_PercentCap          { get; set; }
    public int  TotalSteps  { get; set; }
    public int  PassedSteps { get; set; }
    public bool AllPassed   { get; set; }
}

public sealed class StepResult
{
    public bool    Success { get; init; }
    public string  Message { get; init; } = default!;
    public object? Data    { get; init; }
}
