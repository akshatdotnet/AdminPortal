using Email.Application.Commands;
using Email.Application.Interfaces;
using MediatR;
using Order.Domain.Entities;

namespace Email.Application.EventHandlers;

public sealed class OrderConfirmedEmailHandler(IMediator mediator)
    : INotificationHandler<OrderConfirmedEvent>
{
    public async Task Handle(OrderConfirmedEvent notification, CancellationToken ct)
    {
        var totalStr = notification.Total.ToString("F2");
        await mediator.Send(new SendTemplatedEmailCommand(
            "customer@example.com",
            "Customer",
            EmailTemplate.OrderConfirmation,
            new Dictionary<string, string>
            {
                ["OrderNumber"] = notification.OrderNumber,
                ["Total"] = "$" + totalStr,
                ["OrderUrl"] = "https://shophub.com/orders/" + notification.OrderId
            }), ct);
    }
}

public sealed class OrderShippedEmailHandler(IMediator mediator)
    : INotificationHandler<OrderShippedEvent>
{
    public async Task Handle(OrderShippedEvent notification, CancellationToken ct)
    {
        await mediator.Send(new SendTemplatedEmailCommand(
            "customer@example.com",
            "Customer",
            EmailTemplate.OrderShipped,
            new Dictionary<string, string>
            {
                ["OrderNumber"] = notification.OrderNumber,
                ["TrackingNumber"] = notification.TrackingNumber,
            }), ct);
    }
}

public sealed class OrderCancelledEmailHandler(IMediator mediator)
    : INotificationHandler<OrderCancelledEvent>
{
    public async Task Handle(OrderCancelledEvent notification, CancellationToken ct)
    {
        await mediator.Send(new SendTemplatedEmailCommand(
            "customer@example.com",
            "Customer",
            EmailTemplate.OrderCancelled,
            new Dictionary<string, string>
            {
                ["OrderNumber"] = notification.OrderNumber,
                ["Reason"] = notification.Reason,
                ["RequiresRefund"] = notification.RequiresRefund ? "true" : "false"
            }), ct);
    }
}
