using Common.Domain.Primitives;
using Coupon.Application.Commands;
using Coupon.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace Coupon.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/coupons")]
[ApiVersion("1.0")]
[Produces("application/json")]
public sealed class CouponsController(IMediator mediator) : ControllerBase
{
    [HttpPost("validate")]
    [Authorize]
    [ProducesResponseType(typeof(CouponValidationResult), 200)]
    public async Task<IActionResult> Validate(
        [FromBody] ValidateCouponCommand cmd, CancellationToken ct)
    {
        var r = await mediator.Send(cmd, ct);
        return r.IsSuccess ? Ok(r.Value)
            : Problem(r.Error.Message,
                statusCode: r.Error.Code.Contains("NotFound") ? 404 : 422,
                title: r.Error.Code);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(Guid), 201)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCouponCommand cmd, CancellationToken ct)
    {
        var r = await mediator.Send(cmd, ct);
        return r.IsSuccess ? StatusCode(201, r.Value)
            : Problem(r.Error.Message,
                statusCode: r.Error.Code.Contains("Conflict") ? 409 : 400,
                title: r.Error.Code);
    }
}
