namespace BookingSystem.Core.Entities;

// ═══════════════════════════════════════════════════════════════════════════════
// BASE ENTITY
// ═══════════════════════════════════════════════════════════════════════════════
// All domain entities inherit from this. It provides:
//   Id        → Guid primary key, set once at creation (init-only)
//   CreatedAt → UTC timestamp of creation, immutable
//   UpdatedAt → UTC timestamp of last change, set by entity methods
//
// WHY GUID not int?
//   GUIDs can be generated client-side without a DB round-trip, safe to expose
//   in URLs (no sequential ID enumeration attacks), and work across distributed DBs.
// ═══════════════════════════════════════════════════════════════════════════════
public abstract class BaseEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
}

// ═══════════════════════════════════════════════════════════════════════════════
// CUSTOMER ENTITY
// ═══════════════════════════════════════════════════════════════════════════════
// Represents a person who makes bookings.
// Uses a private constructor + static factory method pattern:
//   ✅ Customer.Create("Rahul", "rahul@example.com", "9876543210")
//   ❌ new Customer { Name = "Rahul" }   ← can't do this, constructor is private
//
// WHY FACTORY METHOD?
//   - Enforces validation at creation time (ArgumentException on null/empty)
//   - Makes the intent clear: "Create a valid Customer"
//   - EF Core uses the private parameterless constructor for materialization
// ═══════════════════════════════════════════════════════════════════════════════
public class Customer : BaseEntity
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string Phone { get; private set; }

    private Customer() { Name = Email = Phone = string.Empty; } // EF Core needs this

    public static Customer Create(string name, string email, string phone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return new Customer { Name = name, Email = email, Phone = phone };
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// VENUE ENTITY
// ═══════════════════════════════════════════════════════════════════════════════
// Represents a bookable location (e.g. "Grand Ballroom Mumbai, capacity 300").
// Capacity is used by IsSlotAvailableAsync to prevent overbooking.
// ═══════════════════════════════════════════════════════════════════════════════
public class Venue : BaseEntity
{
    public string Name { get; private set; }
    public string City { get; private set; }
    public int Capacity { get; private set; }  // max guests at any single slot

    private Venue() { Name = City = string.Empty; }

    public static Venue Create(string name, string city, int capacity) =>
        new() { Name = name, City = city, Capacity = capacity };
}

// ═══════════════════════════════════════════════════════════════════════════════
// BOOKING STATUS — State Machine
// ═══════════════════════════════════════════════════════════════════════════════
// Valid transitions (enforced by Booking methods below):
//
//   Pending  ──[MarkPaid() in ProcessPaymentHandler]──► Confirmed
//   Pending  ──[Cancel()]────────────────────────────► Cancelled
//   Confirmed──[Cancel()]────────────────────────────► Cancelled  (triggers refund)
//   Confirmed──[Complete()]──────────────────────────► Completed
//
// INVALID transitions throw InvalidOperationException:
//   Cancelled → Confirmed  ← "Cannot confirm a cancelled booking"
//   Completed → Cancelled  ← "Only confirmed bookings can be completed"
// ═══════════════════════════════════════════════════════════════════════════════
public enum BookingStatus { Pending, Confirmed, Cancelled, Completed }

// ═══════════════════════════════════════════════════════════════════════════════
// BOOKING ENTITY  — the core aggregate root
// ═══════════════════════════════════════════════════════════════════════════════
// RELATIONSHIPS:
//   Booking ──► Customer  (many bookings per customer)
//   Booking ──► Venue     (many bookings per venue)
//   Booking ──► Order     (exactly one order per booking, created automatically)
//
// ENCAPSULATION RULE:
//   Never set Status directly from outside this class.
//   Always call the state-machine methods: Confirm(), Cancel(), Complete().
//   This ensures business rules are always enforced.
//
//   ✅ booking.Confirm();         → checks status is Pending first
//   ❌ booking.Status = Confirmed → bypasses all checks, never do this
//
// PRICE FORMULA:  TotalAmount = GuestCount × Rs.500
//   Calculated in CreateBookingHandler, stored here for the order.
// ═══════════════════════════════════════════════════════════════════════════════
public class Booking : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public Guid VenueId { get; private set; }
    public DateTime SlotDate { get; private set; }    // the date+time of the booked slot
    public int GuestCount { get; private set; }
    public BookingStatus Status { get; private set; } // controlled by methods below
    public decimal TotalAmount { get; private set; }  // GuestCount × 500
    public string? CancellationReason { get; private set; }

    // Navigation properties — loaded by EF Core when .Include() is used
    public Customer? Customer { get; private set; }
    public Venue? Venue { get; private set; }
    public Order? Order { get; private set; }

    private Booking() { } // EF Core materialisation

    /// <summary>
    /// Creates a new booking in Pending status.
    /// Called by CreateBookingHandler after validating slot availability.
    /// </summary>
    public static Booking Create(Guid customerId, Guid venueId, DateTime slotDate, int guestCount, decimal totalAmount)
    {
        if (guestCount <= 0) throw new ArgumentException("Guest count must be > 0");

        return new Booking
        {
            CustomerId  = customerId,
            VenueId     = venueId,
            SlotDate    = slotDate,
            GuestCount  = guestCount,
            TotalAmount = totalAmount,
            Status      = BookingStatus.Pending   // always starts as Pending
        };
    }

    /// <summary>
    /// Moves booking to Confirmed. Called automatically by ProcessPaymentHandler
    /// after a successful payment. Also callable directly by admin via POST /api/bookings/{id}/confirm.
    /// </summary>
    public void Confirm()
    {
        if (Status != BookingStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm booking in '{Status}' status");
        Status    = BookingStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels the booking. If a paid Order exists, CancelBookingHandler calls
    /// order.Refund() before calling this method. Reason is stored for audit.
    /// </summary>
    public void Cancel(string reason)
    {
        if (Status == BookingStatus.Cancelled)
            throw new InvalidOperationException("Booking already cancelled");
        Status               = BookingStatus.Cancelled;
        CancellationReason   = reason;
        UpdatedAt            = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks booking as Completed (post-event). Only allowed from Confirmed state.
    /// </summary>
    public void Complete()
    {
        if (Status != BookingStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed bookings can be completed");
        Status    = BookingStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ORDER STATUS — State Machine
// ═══════════════════════════════════════════════════════════════════════════════
//   Pending ──[MarkPaid()]────► Paid
//   Pending ──[MarkFailed()]──► Failed    (can retry with a new payment)
//   Paid    ──[Refund()]──────► Refunded  (triggered by booking cancellation)
// ═══════════════════════════════════════════════════════════════════════════════
public enum OrderStatus { Pending, Paid, Refunded, Failed }

// ═══════════════════════════════════════════════════════════════════════════════
// ORDER ENTITY
// ═══════════════════════════════════════════════════════════════════════════════
// An Order is automatically created alongside every Booking (see CreateBookingHandler).
// It represents the financial transaction for that booking.
//
// ONE-TO-ONE with Booking: each booking has exactly one order.
// An Order has one or more OrderItems (line items).
//
// PAYMENT FLOW:
//   POST /api/bookings          → Order created (Status=Pending)
//   POST /api/orders/{id}/pay   → SimulatePaymentGateway() called
//     Success → MarkPaid(ref)   → booking.Confirm() → OrderPaidEvent published
//     Failure → MarkFailed()    → OrderFailedEvent published (booking stays Pending)
// ═══════════════════════════════════════════════════════════════════════════════
public class Order : BaseEntity
{
    public Guid BookingId { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal Amount { get; private set; }
    public OrderStatus Status { get; private set; }
    public string? PaymentReference { get; private set; }  // e.g. "PAY_A1B2C3D4E5F6"

    public Booking? Booking { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    private readonly List<OrderItem> _items = [];

    private Order() { }

    /// <summary>
    /// Creates an Order from a Booking. Called automatically in CreateBookingHandler
    /// right after the Booking entity is created — same DB transaction.
    /// Also adds one OrderItem describing the booking.
    /// </summary>
    public static Order CreateFromBooking(Booking booking)
    {
        var order = new Order
        {
            BookingId  = booking.Id,
            CustomerId = booking.CustomerId,
            Amount     = booking.TotalAmount,
            Status     = OrderStatus.Pending
        };
        // Add a line item describing what was purchased
        order._items.Add(OrderItem.Create(
            order.Id,
            $"Venue Booking: {booking.SlotDate:yyyy-MM-dd}",
            quantity: 1,
            unitPrice: booking.TotalAmount));
        return order;
    }

    /// <summary>Called by ProcessPaymentHandler on gateway success.</summary>
    public void MarkPaid(string paymentReference)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Order is not pending");
        Status           = OrderStatus.Paid;
        PaymentReference = paymentReference;
        UpdatedAt        = DateTime.UtcNow;
    }

    /// <summary>Called by ProcessPaymentHandler on gateway failure.</summary>
    public void MarkFailed()
    {
        Status    = OrderStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Called automatically by CancelBookingHandler if the order was already Paid.
    /// Moves to Refunded so the finance team can process the actual bank refund.
    /// </summary>
    public void Refund()
    {
        Status    = OrderStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ORDER ITEM ENTITY
// ═══════════════════════════════════════════════════════════════════════════════
// Line items within an Order. Currently always 1 item per order
// (the venue booking itself). In a real system you might add:
//   - Catering package: Rs.200/person
//   - AV equipment rental: Rs.5,000
//   - Security deposit: Rs.10,000
// LineTotal is computed (Quantity × UnitPrice), not stored in DB.
// ═══════════════════════════════════════════════════════════════════════════════
public class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public string Description { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal => Quantity * UnitPrice;  // computed, not stored

    private OrderItem() { Description = string.Empty; }

    public static OrderItem Create(Guid orderId, string description, int quantity, decimal unitPrice) =>
        new() { OrderId = orderId, Description = description, Quantity = quantity, UnitPrice = unitPrice };
}
