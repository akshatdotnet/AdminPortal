using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

// ══════════════════════════════════════════════════════════════
// APPLICATION — Commands & Interfaces
// ══════════════════════════════════════════════════════════════
namespace Email.Application;

public sealed record SendEmailCommand(
    string ToEmail,
    string ToName,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null) : IRequest<r>;

public sealed record SendTemplatedEmailCommand(
    string ToEmail,
    string ToName,
    EmailTemplate Template,
    Dictionary<string, string> Variables) : IRequest<r>;

public enum EmailTemplate
{
    WelcomeEmail,
    OrderConfirmation,
    OrderShipped,
    OrderDelivered,
    OrderCancelled,
    PaymentFailed,
    PasswordReset,
    EmailConfirmation,
    RefundProcessed,
    LowStockAlert
}

public interface IEmailService
{
    Task<r> SendAsync(SendEmailCommand command, CancellationToken ct = default);
    Task<r> SendTemplatedAsync(string toEmail, string toName,
        EmailTemplate template, Dictionary<string, string> variables, CancellationToken ct = default);
}

// ── Email Template Engine ─────────────────────────────────────
public static class EmailTemplateEngine
{
    private static readonly Dictionary<EmailTemplate, (string Subject, string HtmlBody)> Templates = new()
    {
        [EmailTemplate.WelcomeEmail] = (
            "Welcome to ShopHub, {{FirstName}}!",
            """
            <h1>Welcome, {{FirstName}}!</h1>
            <p>We're excited to have you join ShopHub. Start exploring our catalog.</p>
            <a href="{{ShopUrl}}" style="background:#4F46E5;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none">
              Shop Now
            </a>
            """),

        [EmailTemplate.OrderConfirmation] = (
            "Order Confirmed — {{OrderNumber}}",
            """
            <h1>Your order is confirmed!</h1>
            <p>Hi {{FirstName}}, we've received your order <strong>{{OrderNumber}}</strong>.</p>
            <table style="width:100%;border-collapse:collapse">
              <tr><td>Order Total</td><td><strong>{{Total}}</strong></td></tr>
              <tr><td>Shipping to</td><td>{{ShippingAddress}}</td></tr>
              <tr><td>Estimated delivery</td><td>{{EstimatedDelivery}}</td></tr>
            </table>
            <a href="{{OrderUrl}}">Track your order</a>
            """),

        [EmailTemplate.OrderShipped] = (
            "Your order {{OrderNumber}} has shipped!",
            """
            <h1>Your order is on the way!</h1>
            <p>Hi {{FirstName}}, your order <strong>{{OrderNumber}}</strong> has shipped.</p>
            <p><strong>Tracking Number:</strong> {{TrackingNumber}}</p>
            <p><strong>Carrier:</strong> {{Carrier}}</p>
            <a href="{{TrackingUrl}}">Track Package</a>
            """),

        [EmailTemplate.OrderCancelled] = (
            "Order {{OrderNumber}} Cancelled",
            """
            <h1>Order Cancelled</h1>
            <p>Hi {{FirstName}}, your order <strong>{{OrderNumber}}</strong> has been cancelled.</p>
            <p><strong>Reason:</strong> {{Reason}}</p>
            <p>{{#if RequiresRefund}}A refund of <strong>{{RefundAmount}}</strong> will be processed within 5-7 business days.{{/if}}</p>
            """),

        [EmailTemplate.PaymentFailed] = (
            "Payment failed for order {{OrderNumber}}",
            """
            <h1>Payment Failed</h1>
            <p>Hi {{FirstName}}, we couldn't process payment for order {{OrderNumber}}.</p>
            <p><strong>Reason:</strong> {{FailureReason}}</p>
            <a href="{{RetryUrl}}">Retry Payment</a>
            """),

        [EmailTemplate.PasswordReset] = (
            "Reset your ShopHub password",
            """
            <h1>Password Reset Request</h1>
            <p>Hi {{FirstName}}, we received a request to reset your password.</p>
            <a href="{{ResetUrl}}" style="background:#4F46E5;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none">
              Reset Password
            </a>
            <p style="color:#888;font-size:12px">This link expires in 1 hour. If you didn't request this, ignore this email.</p>
            """),

        [EmailTemplate.RefundProcessed] = (
            "Refund processed — {{OrderNumber}}",
            """
            <h1>Refund Processed</h1>
            <p>Hi {{FirstName}}, your refund of <strong>{{RefundAmount}}</strong> for order <strong>{{OrderNumber}}</strong> has been processed.</p>
            <p>Please allow 5-7 business days for it to appear in your account.</p>
            """),
    };

    public static (string Subject, string HtmlBody) Render(
        EmailTemplate template, Dictionary<string, string> variables)
    {
        if (!Templates.TryGetValue(template, out var tmpl))
            throw new ArgumentException($"Template '{template}' not found.");

        var subject = tmpl.Subject;
        var body = tmpl.HtmlBody;

        foreach (var (key, value) in variables)
        {
            subject = subject.Replace($"{{{{{key}}}}}", value, StringComparison.OrdinalIgnoreCase);
            body = body.Replace($"{{{{{key}}}}}", value, StringComparison.OrdinalIgnoreCase);
        }

        return (subject, body);
    }
}

// ── Command Handlers ──────────────────────────────────────────
public sealed class SendEmailCommandHandler(IEmailService emailService) :
    IRequestHandler<SendEmailCommand, r>
{
    public Task<r> Handle(SendEmailCommand cmd, CancellationToken ct)
        => emailService.SendAsync(cmd, ct);
}

public sealed class SendTemplatedEmailCommandHandler(IEmailService emailService) :
    IRequestHandler<SendTemplatedEmailCommand, r>
{
    public Task<r> Handle(SendTemplatedEmailCommand cmd, CancellationToken ct)
        => emailService.SendTemplatedAsync(cmd.ToEmail, cmd.ToName, cmd.Template, cmd.Variables, ct);
}

// ══════════════════════════════════════════════════════════════
// INFRASTRUCTURE — SendGrid Implementation
// ══════════════════════════════════════════════════════════════
namespace Email.Infrastructure;

public sealed class SendGridEmailService(
    IOptions<EmailSettings> settings) : Email.Application.IEmailService
{
    private readonly EmailSettings _settings = settings.Value;

    public async Task<r> SendAsync(
        SendEmailCommand cmd, CancellationToken ct = default)
    {
        var client = new SendGridClient(_settings.ApiKey);

        var msg = new SendGridMessage
        {
            From = new EmailAddress(_settings.FromEmail, _settings.FromName),
            Subject = cmd.Subject,
            HtmlContent = WrapInBaseLayout(cmd.HtmlBody),
            PlainTextContent = cmd.PlainTextBody
        };
        msg.AddTo(new EmailAddress(cmd.ToEmail, cmd.ToName));

        var response = await client.SendEmailAsync(msg, ct);
        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(Error.BusinessRule("Email", $"SendGrid returned {response.StatusCode}"));
    }

    public async Task<r> SendTemplatedAsync(
        string toEmail, string toName,
        EmailTemplate template, Dictionary<string, string> variables,
        CancellationToken ct = default)
    {
        var (subject, body) = EmailTemplateEngine.Render(template, variables);
        return await SendAsync(new SendEmailCommand(toEmail, toName, subject, body), ct);
    }

    private static string WrapInBaseLayout(string body) => $"""
        <!DOCTYPE html>
        <html><head><meta charset="utf-8">
        <style>
          body {{ font-family: -apple-system, sans-serif; color: #1a1a1a; margin: 0; padding: 0; }}
          .wrapper {{ max-width: 600px; margin: 0 auto; padding: 40px 20px; }}
          h1 {{ font-size: 24px; font-weight: 600; color: #1a1a1a; }}
          a {{ color: #4F46E5; }}
          table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
          td {{ padding: 8px; border-bottom: 1px solid #e5e7eb; }}
          .footer {{ color: #888; font-size: 12px; margin-top: 40px; text-align: center; }}
        </style>
        </head>
        <body><div class="wrapper">
          {body}
          <div class="footer">
            ShopHub Inc. &middot; You're receiving this because you have an account with us.
            <br><a href="{{{{UnsubscribeUrl}}}}">Unsubscribe</a>
          </div>
        </div></body></html>
        """;
}

public sealed class EmailSettings
{
    public const string SectionName = "EmailSettings";
    public string ApiKey { get; init; } = default!;
    public string FromEmail { get; init; } = default!;
    public string FromName { get; init; } = "ShopHub";
}

// ══════════════════════════════════════════════════════════════
// DOMAIN EVENT HANDLERS — trigger emails on business events
// ══════════════════════════════════════════════════════════════
namespace Email.Application.EventHandlers;

// When order is confirmed → send confirmation email
public sealed class OrderConfirmedEmailHandler(IEmailService emailService) :
    INotificationHandler<Order.Domain.Entities.OrderConfirmedEvent>
{
    public async Task Handle(Order.Domain.Entities.OrderConfirmedEvent notification, CancellationToken ct)
    {
        await emailService.SendTemplatedAsync(
            toEmail: "customer@example.com", // In real app, resolve from CustomerId
            toName: "Customer",
            template: EmailTemplate.OrderConfirmation,
            variables: new Dictionary<string, string>
            {
                ["OrderNumber"] = notification.OrderNumber,
                ["Total"] = $"${notification.Total:F2}",
                ["OrderUrl"] = $"https://shophub.com/orders/{notification.OrderId}"
            }, ct);
    }
}

// When order is shipped → send shipping notification
public sealed class OrderShippedEmailHandler(IEmailService emailService) :
    INotificationHandler<Order.Domain.Entities.OrderShippedEvent>
{
    public async Task Handle(Order.Domain.Entities.OrderShippedEvent notification, CancellationToken ct)
    {
        await emailService.SendTemplatedAsync(
            toEmail: "customer@example.com",
            toName: "Customer",
            template: EmailTemplate.OrderShipped,
            variables: new Dictionary<string, string>
            {
                ["OrderNumber"] = notification.OrderNumber,
                ["TrackingNumber"] = notification.TrackingNumber,
                ["TrackingUrl"] = $"https://shophub.com/track/{notification.TrackingNumber}"
            }, ct);
    }
}

// When user registers → send welcome email
public sealed class UserRegisteredEmailHandler(IEmailService emailService) :
    INotificationHandler<Identity.Domain.Entities.UserRegisteredEvent>
{
    public async Task Handle(Identity.Domain.Entities.UserRegisteredEvent notification, CancellationToken ct)
    {
        await emailService.SendTemplatedAsync(
            toEmail: notification.Email,
            toName: notification.FullName,
            template: EmailTemplate.WelcomeEmail,
            variables: new Dictionary<string, string>
            {
                ["FirstName"] = notification.FullName.Split(' ')[0],
                ["ShopUrl"] = "https://shophub.com"
            }, ct);
    }
}
