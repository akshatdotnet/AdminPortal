using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.Commands;

public class CancelOrderCommandHandler(
    IOrderRepository orders,
    IProductRepository products,
    ICustomerRepository customers,
    IEmailNotificationService email,
    IDomainEventDispatcher dispatcher,
    IUnitOfWork uow,
    ILogger<CancelOrderCommandHandler> logger) : IRequestHandler<CancelOrderCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(CancelOrderCommand cmd, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(cmd.OrderId, ct);
        if (order is null)
            return Result.Failure<OrderDto>("Order not found.");
        if (order.CustomerId != cmd.CustomerId)
            return Result.Failure<OrderDto>("You are not authorized to cancel this order.");

        // Guard before calling domain method — return Result.Failure for all terminal states
        // so callers always get a Result, never an unhandled exception.
        if (order.Status == Domain.Enums.OrderStatus.Cancelled)
            return Result.Failure<OrderDto>("Order is already cancelled.");
        if (order.Status is Domain.Enums.OrderStatus.Shipped or Domain.Enums.OrderStatus.Delivered)
            return Result.Failure<OrderDto>("Cannot cancel an order that has been shipped or delivered.");

        // Release stock reservations
        foreach (var item in order.Items)
        {
            var product = await products.GetByIdAsync(item.ProductId, ct);
            if (product is not null)
                product.ReleaseReservation(item.Quantity);
        }

        order.Cancel(cmd.Reason);

        await uow.SaveChangesAsync(ct);
        await dispatcher.DispatchEventsAsync([order], ct);

        var customer = await customers.GetByIdAsync(cmd.CustomerId, ct);
        if (customer is not null)
        {
            await email.SendAsync(new NotificationMessage(
                customer.Email.Value,
                $"Order Cancelled: {order.OrderNumber}",
                $"Dear {customer.FullName},\n\nYour order {order.OrderNumber} has been cancelled.\n" +
                $"Reason: {cmd.Reason}\n\nRefund of {order.TotalAmount} will be initiated within 3-5 business days."), ct);
        }

        logger.LogInformation("Order {OrderId} cancelled. Reason: {Reason}", cmd.OrderId, cmd.Reason);

        return Result.Success(new OrderDto(
            order.Id, order.OrderNumber, order.CustomerId, order.Status.ToString(),
            order.TotalAmount.Amount, order.TotalAmount.Currency,
            order.Items.Select(i => new OrderItemDto(
                i.ProductId, i.ProductName, i.UnitPrice.Amount, i.Quantity, i.Subtotal.Amount)).ToList(),
            new AddressDto(order.ShippingAddress.Street, order.ShippingAddress.City,
                order.ShippingAddress.State, order.ShippingAddress.PinCode, order.ShippingAddress.Country),
            order.CreatedAt, order.TrackingNumber));
    }
}
