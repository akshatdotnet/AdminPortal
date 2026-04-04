namespace ECommerce.Application.Common.Interfaces;

public record NotificationMessage(string To, string Subject, string Body, bool IsHtml = false);

public interface IEmailNotificationService
{
    Task SendAsync(NotificationMessage message, CancellationToken ct = default);
}

public interface ISmsNotificationService
{
    Task SendAsync(string phoneNumber, string message, CancellationToken ct = default);
}
