using ECommerce.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services.Notification;

/// <summary>
/// Email notification service that prints to console for local dev.
/// Swap with SendGrid/SMTP implementation for production.
/// </summary>
public class ConsoleEmailNotificationService(ILogger<ConsoleEmailNotificationService> logger)
    : IEmailNotificationService
{
    public Task SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL] To: {To} | Subject: {Subject}",
            message.To, message.Subject);

        var original = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n  ✉  EMAIL → {message.To}");
        Console.WriteLine($"     Subject : {message.Subject}");
        Console.WriteLine($"     Body    : {message.Body[..Math.Min(120, message.Body.Length)]}...");
        Console.ForegroundColor = original;

        return Task.CompletedTask;
    }
}
