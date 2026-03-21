using AdminPortal.Application.Interfaces;

namespace AdminPortal.Infrastructure.Services;

/// <summary>
/// Mock email service — writes to console instead of sending real emails.
/// Replace with SmtpEmailService or a SendGrid/Mailgun client in production.
/// </summary>
public class MockEmailService : IEmailService
{
    public Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("════════════════════════════════════════════════");
        Console.WriteLine("  [MOCK EMAIL] Password Reset");
        Console.WriteLine($"  To      : {toEmail}");
        Console.WriteLine($"  User    : {userName}");
        Console.WriteLine($"  Link    : {resetLink}");
        Console.WriteLine("════════════════════════════════════════════════");
        Console.ResetColor();
        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string toEmail, string userName)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  [MOCK EMAIL] Welcome '{userName}' → {toEmail}");
        Console.ResetColor();
        return Task.CompletedTask;
    }
}
