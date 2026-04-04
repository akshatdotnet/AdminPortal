using Common.Domain.Primitives;
using Email.Application.Interfaces;
using MediatR;

namespace Email.Application.Commands;

public sealed record SendEmailCommand(
    string ToEmail, string ToName, string Subject, string HtmlBody) : IRequest<Result>;

public sealed record SendTemplatedEmailCommand(
    string ToEmail, string ToName,
    EmailTemplate Template,
    Dictionary<string, string> Variables) : IRequest<Result>;

public sealed class SendEmailHandler(IEmailService emailService)
    : IRequestHandler<SendEmailCommand, Result>
{
    public Task<Result> Handle(SendEmailCommand cmd, CancellationToken ct)
        => emailService.SendAsync(cmd.ToEmail, cmd.ToName, cmd.Subject, cmd.HtmlBody, ct);
}

public sealed class SendTemplatedEmailHandler(IEmailService emailService)
    : IRequestHandler<SendTemplatedEmailCommand, Result>
{
    public Task<Result> Handle(SendTemplatedEmailCommand cmd, CancellationToken ct)
        => emailService.SendTemplatedAsync(
            cmd.ToEmail, cmd.ToName, cmd.Template, cmd.Variables, ct);
}
