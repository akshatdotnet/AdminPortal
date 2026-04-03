using ECommerce.Domain.Enums;
using ECommerce.Domain.Events;
using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

/// <summary>
/// Order Aggregate Root.
/// Encapsulates all business rules for the order lifecycle.
/// Raises domain events on state transitions.
/// </summary>
public class Order : Entity
{
    private readonly List<OrderItem> _items = [];

    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public Money TotalAmount { get; private set; } = Money.Zero;
    public OrderStatus Status { get; private set; }
    public Address ShippingAddress { get; private set; } = default!;
    public string? TrackingNumber { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation
    public Payment? Payment { get; private set; }

    private Order() { }

    public static Order Create(Guid customerId, IReadOnlyList<CartItem> cartItems, Address shippingAddress)
    {
        if (!cartItems.Any())
            throw new DomainException("Cannot place an order with an empty cart.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            ShippingAddress = shippingAddress,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var item in cartItems)
            order._items.Add(OrderItem.Create(order.Id, item));

        order.TotalAmount = order._items.Aggregate(Money.Zero, (sum, i) => sum + i.Subtotal);
        order.RaiseDomainEvent(new OrderPlacedEvent(order.Id, order.CustomerId, order.TotalAmount));
        return order;
    }

    public void ConfirmPayment()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException($"Cannot confirm payment for order in status '{Status}'.");
        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new OrderConfirmedEvent(Id, CustomerId));
    }

    public void MarkShipped(string trackingNumber)
    {
        if (Status != OrderStatus.Confirmed && Status != OrderStatus.Processing)
            throw new DomainException("Order must be Confirmed or Processing to be shipped.");
        Status = OrderStatus.Shipped;
        TrackingNumber = trackingNumber;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new OrderShippedEvent(Id, CustomerId, trackingNumber));
    }

    public void MarkDelivered()
    {
        if (Status != OrderStatus.Shipped)
            throw new DomainException("Order must be Shipped before it can be Delivered.");
        Status = OrderStatus.Delivered;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered)
            throw new DomainException("Cannot cancel an order that has been shipped or delivered.");
        if (Status == OrderStatus.Cancelled)
            throw new DomainException("Order is already cancelled.");

        CancellationReason = reason;
        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new OrderCancelledEvent(Id, CustomerId, TotalAmount, reason));
    }

    public void MarkRefunded()
    {
        if (Status != OrderStatus.Cancelled)
            throw new DomainException("Only cancelled orders can be refunded.");
        Status = OrderStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new RefundCompletedEvent(Id, CustomerId, TotalAmount));
    }

    private static string GenerateOrderNumber()
        => $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
}
