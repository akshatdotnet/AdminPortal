using Common.Domain.Entities;

namespace Order.Domain.Entities;

public sealed class Order : BaseEntity
{
    private Order() { }
    private readonly List<OrderItem> _items = new();
    private readonly List<OrderStatusHistory> _history = new();

    public string OrderNumber { get; private set; } = default!;
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public PaymentStatus PaymentStatus { get; private set; } = PaymentStatus.Pending;
    public ShippingAddress ShippingAddress { get; private set; } = default!;
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal ShippingCost { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
    public string? CouponCode { get; private set; }
    public string? Notes { get; private set; }
    public string? PaymentIntentId { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? CancellationReason { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _history.AsReadOnly();

    public static Order Create(Guid customerId, ShippingAddress address, string? notes = null)
    {
        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..26].ToUpper(),
            CustomerId = customerId,
            ShippingAddress = address,
            Notes = notes
        };
        order._history.Add(OrderStatusHistory.Create(order.Id, OrderStatus.Pending, "Order placed"));
        order.AddDomainEvent(new OrderPlacedEvent(order.Id, order.OrderNumber, customerId));
        return order;
    }

    public void AddItem(Guid productId, string name, string sku, decimal unitPrice, int qty)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Items can only be added to pending orders.");
        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null) existing.IncreaseQuantity(qty);
        else _items.Add(OrderItem.Create(Id, productId, name, sku, unitPrice, qty));
        Recalculate();
    }

    public void ApplyCoupon(string code, decimal discount)
    {
        CouponCode = code; DiscountAmount = discount; Recalculate();
    }

    public void SetShipping(decimal cost) { ShippingCost = cost; Recalculate(); }
    public void SetTax(decimal tax)        { TaxAmount = tax; Recalculate(); }

    public void ConfirmPayment(string paymentIntentId)
    {
        ValidateTransition(OrderStatus.Confirmed);
        PaymentIntentId = paymentIntentId;
        PaymentStatus = PaymentStatus.Paid;
        PaidAt = DateTime.UtcNow;
        Transition(OrderStatus.Confirmed, $"Payment confirmed: {paymentIntentId}");
        AddDomainEvent(new OrderConfirmedEvent(Id, OrderNumber, CustomerId, Total));
    }

    public void StartProcessing()
    {
        ValidateTransition(OrderStatus.Processing);
        Transition(OrderStatus.Processing, "Order being prepared");
        AddDomainEvent(new OrderProcessingEvent(Id, OrderNumber));
    }

    public void Ship(string trackingNumber, string carrier)
    {
        ValidateTransition(OrderStatus.Shipped);
        TrackingNumber = trackingNumber;
        ShippedAt = DateTime.UtcNow;
        Transition(OrderStatus.Shipped, $"Shipped via {carrier}. Tracking: {trackingNumber}");
        AddDomainEvent(new OrderShippedEvent(Id, OrderNumber, CustomerId, trackingNumber));
    }

    public void Deliver()
    {
        ValidateTransition(OrderStatus.Delivered);
        DeliveredAt = DateTime.UtcNow;
        Transition(OrderStatus.Delivered, "Delivered to customer");
        AddDomainEvent(new OrderDeliveredEvent(Id, OrderNumber, CustomerId));
    }

    public void Cancel(string reason)
    {
        ValidateTransition(OrderStatus.Cancelled);
        CancellationReason = reason;
        CancelledAt = DateTime.UtcNow;
        if (PaymentStatus == PaymentStatus.Paid)
            PaymentStatus = PaymentStatus.RefundPending;
        Transition(OrderStatus.Cancelled, $"Cancelled: {reason}");
        AddDomainEvent(new OrderCancelledEvent(Id, OrderNumber, CustomerId, reason,
            PaymentStatus == PaymentStatus.RefundPending));
    }

    private void Recalculate()
    {
        Subtotal = _items.Sum(i => i.LineTotal);
        Total = Math.Max(0, Subtotal - DiscountAmount + ShippingCost + TaxAmount);
    }

    private void ValidateTransition(OrderStatus target)
    {
        var ok = (Status, target) switch
        {
            (OrderStatus.Pending,    OrderStatus.Confirmed)   => true,
            (OrderStatus.Pending,    OrderStatus.Cancelled)   => true,
            (OrderStatus.Confirmed,  OrderStatus.Processing)  => true,
            (OrderStatus.Confirmed,  OrderStatus.Cancelled)   => true,
            (OrderStatus.Processing, OrderStatus.Shipped)     => true,
            (OrderStatus.Processing, OrderStatus.Cancelled)   => true,
            (OrderStatus.Shipped,    OrderStatus.Delivered)   => true,
            _ => false
        };
        if (!ok) throw new InvalidOperationException(
            $"Invalid transition from {Status} to {target}.");
    }

    private void Transition(OrderStatus s, string note)
    {
        Status = s;
        _history.Add(OrderStatusHistory.Create(Id, s, note));
        SetUpdated("system");
    }
}

public sealed class OrderItem : BaseEntity
{
    private OrderItem() { }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public string Sku { get; private set; } = default!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal => UnitPrice * Quantity;

    internal static OrderItem Create(Guid orderId, Guid productId,
        string name, string sku, decimal price, int qty) =>
        new() { OrderId = orderId, ProductId = productId,
                ProductName = name, Sku = sku, UnitPrice = price, Quantity = qty };

    internal void IncreaseQuantity(int qty) => Quantity += qty;
}

public sealed class OrderStatusHistory : BaseEntity
{
    private OrderStatusHistory() { }
    public Guid OrderId { get; private set; }
    public OrderStatus Status { get; private set; }
    public string Note { get; private set; } = default!;
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

    internal static OrderStatusHistory Create(Guid orderId, OrderStatus status, string note) =>
        new() { OrderId = orderId, Status = status, Note = note };
}

public sealed class ShippingAddress
{
    public string FullName { get; set; } = default!;
    public string Street { get; set; } = default!;
    public string City { get; set; } = default!;
    public string State { get; set; } = default!;
    public string PostalCode { get; set; } = default!;
    public string Country { get; set; } = default!;
    public string Phone { get; set; } = default!;
}

public enum OrderStatus
{
    Pending=1, Confirmed=2, Processing=3,
    Shipped=4, Delivered=5, Cancelled=6, Refunded=7
}

public enum PaymentStatus
{
    Pending=1, Paid=2, Failed=3, RefundPending=4, Refunded=5
}
