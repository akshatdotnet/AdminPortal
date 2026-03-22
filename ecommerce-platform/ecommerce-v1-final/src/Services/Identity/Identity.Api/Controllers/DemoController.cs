using Asp.Versioning;
using Identity.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers;

/// <summary>
/// DEMO — runs the complete Identity workflow in a single HTTP call.
/// Hit POST /api/v1/demo/complete-flow to verify everything works.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/demo")]
[ApiVersion("1.0")]
[Produces("application/json")]
public sealed class DemoController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Runs the FULL Identity flow:
    /// 1. Register Admin
    /// 2. Register Customer
    /// 3. Login as Customer
    /// 4. Refresh access token
    /// 5. Confirm duplicate registration is rejected (409)
    /// Returns step-by-step results so you can verify each one passed.
    /// </summary>
    [HttpPost("complete-flow")]
    [ProducesResponseType(typeof(IdentityDemoResult), 200)]
    public async Task<IActionResult> RunCompleteFlow(CancellationToken ct)
    {
        var ts    = DateTime.UtcNow.Ticks;
        var admin = $"admin_{ts}@shophub.com";
        var cust  = $"customer_{ts}@shophub.com";
        var result = new IdentityDemoResult();

        // ── Step 1: Register Admin ────────────────────────────────────────
        var s1 = await mediator.Send(new RegisterCommand(
            admin, "Admin@123!", "Admin", "User", "+14155550001", "Admin"), ct);
        result.Step1_RegisterAdmin = Step(
            s1.IsSuccess,
            s1.IsSuccess ? $"Admin registered: {admin}" : s1.Error.Message,
            s1.IsSuccess ? (object?)new { s1.Value!.UserId, s1.Value.Role } : null);

        // ── Step 2: Register Customer ─────────────────────────────────────
        var s2 = await mediator.Send(new RegisterCommand(
            cust, "Cust@123!", "John", "Doe", "+14155551234"), ct);
        result.Step2_RegisterCustomer = Step(
            s2.IsSuccess,
            s2.IsSuccess ? $"Customer registered: {cust}" : s2.Error.Message,
            s2.IsSuccess ? (object?)new { s2.Value!.UserId, s2.Value.Role, s2.Value.FullName } : null);

        if (!s2.IsSuccess) return Ok(result);

        // ── Step 3: Login ─────────────────────────────────────────────────
        var s3 = await mediator.Send(new LoginCommand(cust, "Cust@123!"), ct);
        result.Step3_Login = Step(
            s3.IsSuccess,
            s3.IsSuccess ? $"Login OK — token expires {s3.Value!.ExpiresAt:HH:mm:ss} UTC"
                         : s3.Error.Message,
            s3.IsSuccess ? (object?)new { s3.Value!.AccessToken, s3.Value.RefreshToken } : null);

        if (!s3.IsSuccess) return Ok(result);

        // ── Step 4: Refresh Token ─────────────────────────────────────────
        var s4 = await mediator.Send(new RefreshTokenCommand(
            s3.Value!.AccessToken, s3.Value.RefreshToken), ct);
        result.Step4_RefreshToken = Step(
            s4.IsSuccess,
            s4.IsSuccess ? "Token refreshed — new AccessToken issued" : s4.Error.Message,
            s4.IsSuccess ? (object?)new { NewExpiry = s4.Value!.ExpiresAt } : null);

        // ── Step 5: Duplicate registration (must fail 409) ────────────────
        var s5 = await mediator.Send(new RegisterCommand(
            cust, "Cust@123!", "John", "Doe", "+14155551234"), ct);
        result.Step5_DuplicateRejected = Step(
            !s5.IsSuccess,
            !s5.IsSuccess
                ? $"Correctly rejected duplicate email: {s5.Error.Code}"
                : "BUG: duplicate email was accepted!",
            null);

        result.AllPassed =
            result.Step1_RegisterAdmin!.Success &&
            result.Step2_RegisterCustomer!.Success &&
            result.Step3_Login!.Success &&
            result.Step4_RefreshToken!.Success &&
            result.Step5_DuplicateRejected!.Success;

        return Ok(result);
    }

    private static StepResult Step(bool success, string message, object? data) =>
        new() { Success = success, Message = message, Data = data };
}

public sealed class IdentityDemoResult
{
    public StepResult? Step1_RegisterAdmin        { get; set; }
    public StepResult? Step2_RegisterCustomer     { get; set; }
    public StepResult? Step3_Login                { get; set; }
    public StepResult? Step4_RefreshToken         { get; set; }
    public StepResult? Step5_DuplicateRejected    { get; set; }
    public bool        AllPassed                  { get; set; }
}

public sealed class StepResult
{
    public bool    Success { get; init; }
    public string  Message { get; init; } = default!;
    public object? Data    { get; init; }
}
