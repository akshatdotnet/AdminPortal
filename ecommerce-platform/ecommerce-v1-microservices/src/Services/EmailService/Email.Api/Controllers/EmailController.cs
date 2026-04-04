using Asp.Versioning;
using Email.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Email.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/email")]
[ApiVersion("1.0")]
[Authorize(Policy = "AdminOnly")]
[Produces("application/json")]
public sealed class EmailController(IMediator mediator) : ControllerBase
{
    [HttpPost("send")]
    public async Task<IActionResult> Send(
        [FromBody] SendEmailCommand cmd, CancellationToken ct)
    {
        var r = await mediator.Send(cmd, ct);
        return r.IsSuccess ? Ok(new { Message = "Email sent." })
            : Problem(r.Error.Message);
    }

    [HttpPost("send-template")]
    public async Task<IActionResult> SendTemplate(
        [FromBody] SendTemplatedEmailCommand cmd, CancellationToken ct)
    {
        var r = await mediator.Send(cmd, ct);
        return r.IsSuccess ? Ok(new { Message = "Templated email sent." })
            : Problem(r.Error.Message);
    }
}
