using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderEntity = ECommerce.Domain.Entities.Order;

namespace ECommerce.Application.Orders.Commands;

public class PlaceOrderCommandHandler(
    IOrderRepository orders,
    ICartRepository carts,
    ICartItemRepository cartItems,
    IProductRepository products,
    ICustomerRepository customers,
    IEmailNotificationService email,
    IDomainEventDispatcher dispatcher,
    IUnitOfWork uow,
    ILogger<PlaceOrderCommandHandler> logger) : IRequestHandler<PlaceOrderCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(PlaceOrderCommand cmd, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(cmd.CustomerId, ct);
        if (customer is null)
            return Result.Failure<OrderDto>("Customer not found.");

        var cart = await carts.GetByCustomerIdAsync(cmd.CustomerId, ct);
        if (cart is null || !cart.Items.Any())
            return Result.Failure<OrderDto>("Cart is empty. Add items before placing an order.");

        var cartItemSnapshot = cart.Items.ToList();

        foreach (var item in cartItemSnapshot)
        {
            var product = await products.GetByIdAsync(item.ProductId, ct);
            if (product is null)
                return Result.Failure<OrderDto>($"Product '{item.ProductName}' is no longer available.");
            if (product.AvailableQuantity < item.Quantity)
                return Result.Failure<OrderDto>($"Insufficient stock for '{item.ProductName}'. Available: {product.AvailableQuantity}");
            product.Reserve(item.Quantity);
        }

        var address = new Address(
            cmd.ShippingAddress.Street, cmd.ShippingAddress.City,
            cmd.ShippingAddress.State, cmd.ShippingAddress.PinCode, cmd.ShippingAddress.Country);

        var order = OrderEntity.Create(cmd.CustomerId, cartItemSnapshot, address);
        await orders.AddAsync(order, ct);

        // Explicitly delete all CartItem rows (EF no longer manages this via navigation cascade)
        foreach (var item in cartItemSnapshot)
            cartItems.Remove(item);

        // Update cart timestamp
        cart.Touch();

        await uow.SaveChangesAsync(ct);
        await dispatcher.DispatchEventsAsync([order], ct);

        await email.SendAsync(new NotificationMessage(
            customer.Email.Value,
            $"Order Confirmed: {order.OrderNumber}",
            $"Dear {customer.FullName},\n\nYour order {order.OrderNumber} has been placed.\n" +
            $"Total: {order.TotalAmount}\n\nThank you for shopping with us!"), ct);

        logger.LogInformation("Order {OrderNumber} placed for customer {CustomerId}",
            order.OrderNumber, cmd.CustomerId);
        return Result.Success(MapToDto(order));
    }

    private static OrderDto MapToDto(OrderEntity o) => new(
        o.Id, o.OrderNumber, o.CustomerId, o.Status.ToString(),
        o.TotalAmount.Amount, o.TotalAmount.Currency,
        o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName,
            i.UnitPrice.Amount, i.Quantity, i.Subtotal.Amount)).ToList(),
        new AddressDto(o.ShippingAddress.Street, o.ShippingAddress.City,
            o.ShippingAddress.State, o.ShippingAddress.PinCode, o.ShippingAddress.Country),
        o.CreatedAt, o.TrackingNumber);
}
