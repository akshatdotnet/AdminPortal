using Common.Domain.Entities;

namespace Order.Domain.Entities;

// ══════════════════════════════════════════════════════════════
// ORDER AGGREGATE ROOT — implements state machine for lifecycle
// ══════════════════════════════════════════════════════════════
public sealed class Order : BaseEntity
{
    private Order() { }

    private readonly List<OrderItem> _items = [];
    private readonly List<OrderStatusHistory> _history = [];

    public string OrderNumber { get; private set; } = default!;
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public ShippingAddress ShippingAddress { get; private set; } = default!;
    public Money Subtotal { get; private set; } = Money.Zero;
    public Money DiscountAmount { get; private set; } = Money.Zero;
    public Money ShippingCost { get; private set; } = Money.Zero;
    public Money TaxAmount { get; private set; } = Money.Zero;
    public Money Total { get; private set; } = Money.Zero;
    public string? CouponCode { get; private set; }
    public string? Notes { get; private set; }
    public string? PaymentIntentId { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? CancellationReason { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _history.AsReadOnly();

    // ── Factory ────────────────────────────────────────────────
    public static Order Create(Guid customerId, ShippingAddress shippingAddress, string? notes = null)
    {
        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            CustomerId = customerId,
            ShippingAddress = shippingAddress,
            Notes = notes
        };

        order._history.Add(OrderStatusHistory.Create(order.Id, OrderStatus.Pending, "Order placed"));
        order.AddDomainEvent(new OrderPlacedEvent(order.Id, order.OrderNumber, customerId));
        return order;
    }

    // ── Item management ────────────────────────────────────────
    public void AddItem(Guid productId, string productName, string sku,
        decimal unitPrice, string currency, int quantity)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Items can only be added to pending orders.");

        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
            existing.IncreaseQuantity(quantity);
        else
            _items.Add(OrderItem.Create(Id, productId, productName, sku, unitPrice, currency, quantity));

        RecalculateTotals();
    }

    public void ApplyCoupon(string couponCode, decimal discountAmount, string currency)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Coupon can only be applied to pending orders.");

        CouponCode = couponCode;
        DiscountAmount = new Money(discountAmount, currency);
        RecalculateTotals();
    }

    public void SetShipping(decimal shippingCost, string currency)
    {
        ShippingCost = new Money(shippingCost, currency);
        RecalculateTotals();
    }

    public void SetTax(decimal taxAmount, string currency)
    {
        TaxAmount = new Money(taxAmount, currency);
        RecalculateTotals();
    }

    // ── State machine transitions ──────────────────────────────
    public void ConfirmPayment(string paymentIntentId)
    {
        ValidateTransition(OrderStatus.Confirmed);
        PaymentIntentId = paymentIntentId;
        PaymentStatus = PaymentStatus.Paid;
        PaidAt = DateTime.UtcNow;
        TransitionTo(OrderStatus.Confirmed, $"Payment confirmed: {paymentIntentId}");
        AddDomainEvent(new OrderConfirmedEvent(Id, OrderNumber, CustomerId, Total.Amount));
    }

    public void StartProcessing()
    {
        ValidateTransition(OrderStatus.Processing);
        TransitionTo(OrderStatus.Processing, "Order is being prepared");
        AddDomainEvent(new OrderProcessingEvent(Id, OrderNumber));
    }

    public void Ship(string trackingNumber, string carrier)
    {
        ValidateTransition(OrderStatus.Shipped);
        TrackingNumber = trackingNumber;
        ShippedAt = DateTime.UtcNow;
        TransitionTo(OrderStatus.Shipped, $"Shipped via {carrier}, tracking: {trackingNumber}");
        AddDomainEvent(new OrderShippedEvent(Id, OrderNumber, CustomerId, trackingNumber));
    }

    public void Deliver()
    {
        ValidateTransition(OrderStatus.Delivered);
        DeliveredAt = DateTime.UtcNow;
        TransitionTo(OrderStatus.Delivered, "Package delivered");
        AddDomainEvent(new OrderDeliveredEvent(Id, OrderNumber, CustomerId));
    }

    public void Cancel(string reason)
    {
        ValidateTransition(OrderStatus.Cancelled);
        CancellationReason = reason;
        CancelledAt = DateTime.UtcNow;

        if (PaymentStatus == PaymentStatus.Paid)
            PaymentStatus = PaymentStatus.RefundPending;

        TransitionTo(OrderStatus.Cancelled, $"Cancelled: {reason}");
        AddDomainEvent(new OrderCancelledEvent(Id, OrderNumber, CustomerId, reason,
            PaymentStatus == PaymentStatus.RefundPending));
    }

    public void CompleteRefund()
    {
        if (PaymentStatus != PaymentStatus.RefundPending)
            throw new InvalidOperationException("No pending refund.");
        PaymentStatus = PaymentStatus.Refunded;
    }

    // ── Private helpers ────────────────────────────────────────
    private void RecalculateTotals()
    {
        Subtotal = _items.Aggregate(Money.Zero,
            (acc, item) => new Money(acc.Amount + item.LineTotal, "USD"));
        Total = new Money(
            Subtotal.Amount - DiscountAmount.Amount + ShippingCost.Amount + TaxAmount.Amount,
            "USD");
    }

    private void ValidateTransition(OrderStatus target)
    {
        var valid = (Status, target) switch
        {
            (OrderStatus.Pending, OrderStatus.Confirmed) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.Confirmed, OrderStatus.Processing) => true,
            (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
            (OrderStatus.Processing, OrderStatus.Shipped) => true,
            (OrderStatus.Processing, OrderStatus.Cancelled) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            _ => false
        };

        if (!valid)
            throw new InvalidOperationException(
                $"Invalid transition from {Status} to {target}.");
    }

    private void TransitionTo(OrderStatus newStatus, string note)
    {
        Status = newStatus;
        _history.Add(OrderStatusHistory.Create(Id, newStatus, note));
        SetUpdated("system");
    }

    private static string GenerateOrderNumber() =>
        $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
}

// ══════════════════════════════════════════════════════════════
// ORDER ITEM ENTITY
// ══════════════════════════════════════════════════════════════
public sealed class OrderItem : BaseEntity
{
    private OrderItem() { }

    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public string Sku { get; private set; } = default!;
    public decimal UnitPrice { get; private set; }
    public string Currency { get; private set; } = "USD";
    public int Quantity { get; private set; }
    public decimal LineTotal => UnitPrice * Quantity;

    internal static OrderItem Create(Guid orderId, Guid productId, string name,
        string sku, decimal unitPrice, string currency, int quantity) =>
        new()
        {
            OrderId = orderId, ProductId = productId, ProductName = name,
            Sku = sku, UnitPrice = unitPrice, Currency = currency, Quantity = quantity
        };

    internal void IncreaseQuantity(int qty) => Quantity += qty;
}

// ══════════════════════════════════════════════════════════════
// ORDER STATUS HISTORY
// ══════════════════════════════════════════════════════════════
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

// ══════════════════════════════════════════════════════════════
// VALUE OBJECTS AND ENUMS
// ══════════════════════════════════════════════════════════════
public sealed record ShippingAddress(
    string FullName, string Street, string City,
    string State, string PostalCode, string Country, string Phone);

public sealed record Money(decimal Amount, string Currency = "USD")
{
    public static readonly Money Zero = new(0);
    public static Money operator +(Money a, Money b) => new(a.Amount + b.Amount, a.Currency);
}

public enum OrderStatus
{
    Pending = 1,
    Confirmed = 2,
    Processing = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6,
    Refunded = 7
}

public enum PaymentStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
    RefundPending = 4,
    Refunded = 5
}
