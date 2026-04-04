using Common.Domain.Primitives;

namespace Email.Application.Interfaces;

public interface IEmailService
{
    Task<Result> SendAsync(string toEmail, string toName, string subject,
        string htmlBody, CancellationToken ct = default);

    Task<Result> SendTemplatedAsync(string toEmail, string toName,
        EmailTemplate template, Dictionary<string, string> variables,
        CancellationToken ct = default);
}

public enum EmailTemplate
{
    WelcomeEmail, OrderConfirmation, OrderShipped,
    OrderDelivered, OrderCancelled, PasswordReset,
    PaymentFailed, RefundProcessed
}
