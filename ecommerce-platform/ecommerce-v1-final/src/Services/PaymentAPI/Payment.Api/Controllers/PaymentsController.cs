using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Commands;
using Payment.Application.DTOs;
using System.Security.Claims;
using Asp.Versioning;

namespace Payment.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/payments")]
[ApiVersion("1.0")]
[Produces("application/json")]
public sealed class PaymentsController(IMediator mediator) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("sessions")]
    [Authorize]
    [ProducesResponseType(typeof(PaymentSessionDto), 200)]
    public async Task<IActionResult> CreateSession(
        [FromBody] CreatePaymentSessionCommand cmd, CancellationToken ct)
    {
        var r = await mediator.Send(cmd, ct);
        return r.IsSuccess ? Ok(r.Value)
            : Problem(r.Error.Message, statusCode: 400, title: r.Error.Code);
    }

    [HttpPost("webhook/{gateway}")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(
        string gateway, CancellationToken ct)
    {
        var payload = await new StreamReader(Request.Body).ReadToEndAsync(ct);
        var sig = Request.Headers["Stripe-Signature"].ToString();
        var r = await mediator.Send(new HandleWebhookCommand(gateway, payload, sig), ct);
        return r.IsSuccess ? Ok() : BadRequest(r.Error.Message);
    }

    [HttpPost("{id:guid}/refund")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Refund(
        Guid id, [FromBody] RefundRequest req, CancellationToken ct)
    {
        var r = await mediator.Send(
            new InitiateRefundCommand(id, req.Amount, req.Reason), ct);
        return r.IsSuccess ? NoContent()
            : Problem(r.Error.Message, statusCode: 400, title: r.Error.Code);
    }
}

public sealed record RefundRequest(decimal Amount, string Reason);
