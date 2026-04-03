using ECommerce.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services.Notification;

/// <summary>
/// SMS notification service that prints to console for local dev.
/// Replace with Twilio / AWS SNS in production.
/// </summary>
public class ConsoleSmsNotificationService(ILogger<ConsoleSmsNotificationService> logger)
    : ISmsNotificationService
{
    public Task SendAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        logger.LogInformation("[SMS] To: {Phone} | Message: {Msg}", phoneNumber, message);

        var original = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n  💬  SMS → {phoneNumber}");
        Console.WriteLine($"     {message}");
        Console.ForegroundColor = original;

        return Task.CompletedTask;
    }
}
