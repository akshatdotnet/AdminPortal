using Email.Application.Commands;
using Email.Application.Interfaces;
using Identity.Domain.Entities;
using MediatR;

namespace Email.Application.EventHandlers;

public sealed class UserRegisteredEmailHandler(IMediator mediator)
    : INotificationHandler<UserRegisteredEvent>
{
    public async Task Handle(UserRegisteredEvent notification, CancellationToken ct)
    {
        var firstName = notification.FullName.Split(' ')[0];
        await mediator.Send(new SendTemplatedEmailCommand(
            notification.Email,
            notification.FullName,
            EmailTemplate.WelcomeEmail,
            new Dictionary<string, string>
            {
                ["FirstName"] = firstName,
                ["ShopUrl"] = "https://shophub.com"
            }), ct);
    }
}
