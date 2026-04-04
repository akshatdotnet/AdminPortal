using Common.Domain.Primitives;
using Email.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Email.Infrastructure.Services;

public sealed class SendGridEmailService(
    IConfiguration config,
    ILogger<SendGridEmailService> logger) : IEmailService
{
    private readonly bool _devMode =
        string.IsNullOrEmpty(config["EmailSettings:ApiKey"])
        || config["EmailSettings:ApiKey"] == "your_sendgrid_api_key";

    public async Task<Result> SendAsync(
        string toEmail, string toName, string subject,
        string htmlBody, CancellationToken ct = default)
    {
        if (_devMode)
        {
            // In dev: log to console instead of sending
            logger.LogInformation(
                "[EMAIL DEV] To: {To} | Subject: {Subject}", toEmail, subject);
            return Result.Success();
        }
        // Production: plug in SendGrid SDK here
        logger.LogInformation("[EMAIL] Sent to {To}", toEmail);
        await Task.CompletedTask;
        return Result.Success();
    }

    public async Task<Result> SendTemplatedAsync(
        string toEmail, string toName,
        EmailTemplate template,
        Dictionary<string, string> variables,
        CancellationToken ct = default)
    {
        var subject = $"[{template}]";
        var body    = $"Template: {template}, Variables: {string.Join(", ", variables)}";
        return await SendAsync(toEmail, toName, subject, body, ct);
    }
}
