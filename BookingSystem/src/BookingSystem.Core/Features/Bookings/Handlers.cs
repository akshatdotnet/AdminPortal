using BookingSystem.Core.Entities;
using BookingSystem.Core.Events;
using BookingSystem.Core.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BookingSystem.Core.Features.Bookings;

// ═══════════════════════════════════════════════════════════════════════════════
// CREATE BOOKING HANDLER
// ═══════════════════════════════════════════════════════════════════════════════
// TRIGGERED BY: POST /api/bookings
// FULL FLOW THIS HANDLER EXECUTES:
//
//   [1] FluentValidation pipeline checks inputs (customerId, venueId, guestCount)
//   [2] Load Customer from DB (or 404 if not found)
//   [3] Load Venue from DB (or 404 if not found)
//   [4] Invalidate slot cache (so next slot check is fresh)
//   [5] Check slot availability: sum existing guests + new guests ≤ venue capacity
//   [6] Calculate price: guestCount × Rs.500
//   [7] booking = Booking.Create(...)           → new entity, Status=Pending
//   [8] order   = Order.CreateFromBooking(...)  → new entity, Status=Pending
//   [9] uow.SaveChangesAsync()                  → ONE transaction, both rows saved
//   [10] eventBus.PublishAsync(BookingCreatedEvent) → feeds EventBridge
//          └─► EmailWorker sees it   → sends "booking request received" email
//          └─► AnalyticsWorker sees it → totalBookings++, totalRevenue += amount
//   [11] Return BookingDto to API → HTTP 201 Created
//
// WHAT HAPPENS NEXT (outside this handler):
//   Customer calls POST /api/orders/{orderId}/pay → ProcessPaymentHandler
// ═══════════════════════════════════════════════════════════════════════════════
public class CreateBookingHandler(
    IUnitOfWork uow,
    IEventBus eventBus,
    ICacheService cache,
    ILogger<CreateBookingHandler> logger)
    : IRequestHandler<CreateBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(CreateBookingCommand cmd, CancellationToken ct)
    {
        logger.LogInformation(
            "Creating booking for Customer={CustomerId} Venue={VenueId} Date={Date}",
            cmd.CustomerId, cmd.VenueId, cmd.SlotDate);

        // ── Step 1: Load and validate customer exists ──────────────────────────
        var customer = await uow.Customers.GetByIdAsync(cmd.CustomerId, ct)
            ?? throw new KeyNotFoundException($"Customer {cmd.CustomerId} not found");

        // ── Step 2: Load and validate venue exists ────────────────────────────
        var venue = await uow.Venues.GetByIdAsync(cmd.VenueId, ct)
            ?? throw new KeyNotFoundException($"Venue {cmd.VenueId} not found");

        // ── Step 3: Invalidate the slot cache so the availability check is fresh
        //   Cache key format: "slots:{venueId}:{yyyyMMdd}"
        var cacheKey = $"slots:{cmd.VenueId}:{cmd.SlotDate:yyyyMMdd}";
        await cache.RemoveAsync(cacheKey, ct);

        // ── Step 4: Check capacity — sum of existing guests + new guests ≤ capacity
        //   Query: SELECT SUM(GuestCount) FROM Bookings WHERE VenueId=? AND SlotDate=? AND Status!='Cancelled'
        bool available = await uow.Bookings.IsSlotAvailableAsync(
            cmd.VenueId, cmd.SlotDate, cmd.GuestCount, ct);

        if (!available)
            throw new InvalidOperationException(
                $"Slot {cmd.SlotDate:yyyy-MM-dd HH:mm} is not available at {venue.Name}. " +
                $"Try a different time or reduce guest count.");

        // ── Step 5: Calculate price ────────────────────────────────────────────
        decimal amount = cmd.GuestCount * 500m;  // Rs.500 per guest

        // ── Step 6: Create domain entities ────────────────────────────────────
        // Booking starts as Pending — it moves to Confirmed only after payment
        var booking = Booking.Create(cmd.CustomerId, cmd.VenueId, cmd.SlotDate, cmd.GuestCount, amount);

        // Order is created atomically with the booking — same transaction
        // This ensures we never have a booking without a corresponding order
        var order = Order.CreateFromBooking(booking);

        await uow.Bookings.AddAsync(booking, ct);
        await uow.Orders.AddAsync(order, ct);

        // ── Step 7: Persist in ONE database transaction ────────────────────────
        // UnitOfWork.SaveChangesAsync() calls DbContext.SaveChangesAsync()
        // If either insert fails, both are rolled back (atomicity)
        await uow.SaveChangesAsync(ct);

        // ── Step 8: Publish domain event (fire-and-forget) ────────────────────
        // This does NOT block the response. The HTTP 201 is returned to the client
        // while workers process the event asynchronously in the background.
        await eventBus.PublishAsync(new BookingCreatedEvent(
            BookingId:  booking.Id,
            CustomerId: customer.Id,
            VenueId:    venue.Id,
            SlotDate:   booking.SlotDate,
            Amount:     booking.TotalAmount,
            OccurredAt: DateTime.UtcNow), ct);

        logger.LogInformation(
            "Booking {BookingId} created with Order {OrderId}",
            booking.Id, order.Id);

        return ToDto(booking, customer, venue);
    }

    private static BookingDto ToDto(Booking b, Customer c, Venue v) =>
        new(b.Id, b.CustomerId, c.Name, b.VenueId, v.Name,
            b.SlotDate, b.GuestCount, b.Status, b.TotalAmount, b.CreatedAt);
}

// ═══════════════════════════════════════════════════════════════════════════════
// CONFIRM BOOKING HANDLER
// ═══════════════════════════════════════════════════════════════════════════════
// TRIGGERED BY: POST /api/bookings/{id}/confirm  (admin/manual confirmation)
// NOTE: Confirmation also happens automatically inside ProcessPaymentHandler
//       after successful payment — you rarely need to call this manually.
//
// FLOW:
//   Load booking → booking.Confirm() → SaveChanges → publish BookingConfirmedEvent
//   EmailWorker sees BookingConfirmedEvent → sends "slot confirmed" email
// ═══════════════════════════════════════════════════════════════════════════════
public class ConfirmBookingHandler(
    IUnitOfWork uow,
    IEventBus eventBus,
    ILogger<ConfirmBookingHandler> logger)
    : IRequestHandler<ConfirmBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(ConfirmBookingCommand cmd, CancellationToken ct)
    {
        var booking = await uow.Bookings.GetByIdAsync(cmd.BookingId, ct)
            ?? throw new KeyNotFoundException($"Booking {cmd.BookingId} not found");

        // booking.Confirm() enforces the state machine:
        // throws InvalidOperationException if status != Pending
        booking.Confirm();
        await uow.SaveChangesAsync(ct);

        var customer = await uow.Customers.GetByIdAsync(booking.CustomerId, ct);
        var venue    = await uow.Venues.GetByIdAsync(booking.VenueId, ct);

        // Publish event → EmailWorker sends "Your booking is confirmed" email
        await eventBus.PublishAsync(new BookingConfirmedEvent(
            BookingId:     booking.Id,
            CustomerId:    booking.CustomerId,
            CustomerEmail: customer?.Email ?? "",
            SlotDate:      booking.SlotDate,
            OccurredAt:    DateTime.UtcNow), ct);

        logger.LogInformation("Booking {BookingId} confirmed", booking.Id);
        return ToDto(booking, customer!, venue!);
    }

    private static BookingDto ToDto(Booking b, Customer c, Venue v) =>
        new(b.Id, b.CustomerId, c.Name, b.VenueId, v.Name,
            b.SlotDate, b.GuestCount, b.Status, b.TotalAmount, b.CreatedAt);
}

// ═══════════════════════════════════════════════════════════════════════════════
// CANCEL BOOKING HANDLER
// ═══════════════════════════════════════════════════════════════════════════════
// TRIGGERED BY: POST /api/bookings/{id}/cancel  { "reason": "..." }
//
// SMART CANCELLATION — AUTO REFUND:
//   If the order for this booking was already Paid:
//     order.Refund() is called automatically before cancelling the booking.
//   Then both changes are saved in one transaction.
//
// FLOW:
//   Load booking
//   └── If order.Status == Paid → order.Refund()
//   booking.Cancel(reason)
//   SaveChanges (single transaction — booking + order in one commit)
//   Publish BookingCancelledEvent
//   EmailWorker → "Your booking was cancelled, refund coming in 3-5 days"
//   AnalyticsWorker → cancellations++
// ═══════════════════════════════════════════════════════════════════════════════
public class CancelBookingHandler(
    IUnitOfWork uow,
    IEventBus eventBus,
    ILogger<CancelBookingHandler> logger)
    : IRequestHandler<CancelBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(CancelBookingCommand cmd, CancellationToken ct)
    {
        var booking = await uow.Bookings.GetByIdAsync(cmd.BookingId, ct)
            ?? throw new KeyNotFoundException($"Booking {cmd.BookingId} not found");

        // Check if there is a paid order that needs refunding
        var order = await uow.Orders.GetByBookingIdAsync(booking.Id, ct);
        if (order?.Status == OrderStatus.Paid)
        {
            // Auto-refund: move order to Refunded status
            // The finance team would then process the actual bank refund
            order.Refund();
            await uow.Orders.UpdateAsync(order, ct);
            logger.LogInformation(
                "Order {OrderId} auto-refunded due to booking cancellation", order.Id);
        }

        // Cancel the booking (state machine enforces valid transitions)
        booking.Cancel(cmd.Reason);
        await uow.SaveChangesAsync(ct);  // saves both order refund + booking cancel atomically

        // Publish event → EmailWorker sends cancellation email
        // → AnalyticsWorker increments cancellation counter
        await eventBus.PublishAsync(new BookingCancelledEvent(
            BookingId:  booking.Id,
            CustomerId: booking.CustomerId,
            Reason:     cmd.Reason,
            OccurredAt: DateTime.UtcNow), ct);

        var customer = await uow.Customers.GetByIdAsync(booking.CustomerId, ct);
        var venue    = await uow.Venues.GetByIdAsync(booking.VenueId, ct);
        return new(booking.Id, booking.CustomerId, customer!.Name, booking.VenueId,
            venue!.Name, booking.SlotDate, booking.GuestCount,
            booking.Status, booking.TotalAmount, booking.CreatedAt);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// PROCESS PAYMENT HANDLER
// ═══════════════════════════════════════════════════════════════════════════════
// TRIGGERED BY: POST /api/orders/{id}/pay  { "cardToken": "..." }
//
// THIS IS THE MOST IMPORTANT HANDLER — it is where money changes hands.
//
// FLOW (SUCCESS PATH):
//   Load order → SimulatePaymentGateway(cardToken, amount)
//   → Gateway returns (success=true, ref="PAY_XXXX")
//   → order.MarkPaid("PAY_XXXX")
//   → booking.Confirm()         ← slot is now locked
//   → SaveChanges               ← single transaction (order + booking)
//   → Publish OrderPaidEvent
//        └─► EmailWorker    → "Payment confirmed - Ref: PAY_XXXX"
//        └─► AnalyticsWorker → paidOrders++
//
// FLOW (FAILURE PATH):
//   → Gateway returns (success=false)
//   → order.MarkFailed()
//   → SaveChanges
//   → Publish OrderFailedEvent
//        └─► EmailWorker → "Payment failed - please retry"
//        └─► AnalyticsWorker → failedPayments++
//
// TESTING PAYMENT OUTCOMES:
//   cardToken = "valid_token"  → SUCCESS  (booking auto-confirmed)
//   cardToken = "fail_card"    → FAILURE  (booking stays Pending, retryable)
//   Any token starting with "fail_" → FAILURE
// ═══════════════════════════════════════════════════════════════════════════════
public class ProcessPaymentHandler(
    IUnitOfWork uow,
    IEventBus eventBus,
    ILogger<ProcessPaymentHandler> logger)
    : IRequestHandler<ProcessPaymentCommand, OrderDto>
{
    public async Task<OrderDto> Handle(ProcessPaymentCommand cmd, CancellationToken ct)
    {
        var order = await uow.Orders.GetByIdAsync(cmd.OrderId, ct)
            ?? throw new KeyNotFoundException($"Order {cmd.OrderId} not found");

        logger.LogInformation(
            "Processing payment for Order {OrderId}, Amount=Rs.{Amount}",
            order.Id, order.Amount);

        // ── Call payment gateway (simulated) ──────────────────────────────────
        // In production: replace with Razorpay / Stripe / PayU SDK
        //   var razorpay = new RazorpayClient(key, secret);
        //   var payment  = razorpay.Payment.Fetch(cardToken);
        var (success, reference) = await SimulatePaymentGatewayAsync(
            cmd.CardToken, order.Amount, ct);

        if (success)
        {
            // ── SUCCESS: mark order paid and auto-confirm the booking ──────────
            order.MarkPaid(reference!);
            await uow.Orders.UpdateAsync(order, ct);

            // Auto-confirm booking — no need to call POST /api/bookings/{id}/confirm
            var booking = await uow.Bookings.GetByIdAsync(order.BookingId, ct);
            booking!.Confirm();
            await uow.Bookings.UpdateAsync(booking, ct);

            // Both order (Paid) and booking (Confirmed) saved in one transaction
            await uow.SaveChangesAsync(ct);

            // Publish event → EmailWorker + AnalyticsWorker respond async
            await eventBus.PublishAsync(new OrderPaidEvent(
                OrderId:          order.Id,
                BookingId:        order.BookingId,
                PaymentReference: reference!,
                OccurredAt:       DateTime.UtcNow), ct);

            logger.LogInformation(
                "Payment SUCCESS for Order {OrderId}, Ref={Ref}", order.Id, reference);
        }
        else
        {
            // ── FAILURE: mark order failed, booking stays Pending (retryable) ──
            order.MarkFailed();
            await uow.Orders.UpdateAsync(order, ct);
            await uow.SaveChangesAsync(ct);

            await eventBus.PublishAsync(new OrderFailedEvent(
                OrderId:   order.Id,
                BookingId: order.BookingId,
                Reason:    "Card declined",
                OccurredAt: DateTime.UtcNow), ct);

            logger.LogWarning("Payment FAILED for Order {OrderId}", order.Id);
        }

        return new(order.Id, order.BookingId, order.Amount,
            order.Status, order.PaymentReference, order.CreatedAt);
    }

    /// <summary>
    /// Simulates a real payment gateway with 200ms network latency.
    /// SUCCESS: any cardToken that does NOT start with "fail_"
    /// FAILURE: any cardToken starting with "fail_" (e.g. "fail_card", "fail_test")
    /// </summary>
    private static async Task<(bool success, string? reference)> SimulatePaymentGatewayAsync(
        string token, decimal amount, CancellationToken ct)
    {
        await Task.Delay(200, ct); // simulate gateway network round-trip
        bool success = !token.StartsWith("fail_", StringComparison.OrdinalIgnoreCase);
        string? reference = success
            ? $"PAY_{Guid.NewGuid():N}"[..16].ToUpper()  // e.g. "PAY_A1B2C3D4E5F6G7"
            : null;
        return (success, reference);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// QUERY HANDLERS — Read-only, cache-first, no side effects
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// GET /api/bookings/{id}
/// Cache-Aside: check memory cache first (key="booking:{guid}"), fall back to DB.
/// Stores result for 5 minutes. Cache is invalidated when booking changes.
/// </summary>
public class GetBookingHandler(IUnitOfWork uow, ICacheService cache)
    : IRequestHandler<GetBookingQuery, BookingDto?>
{
    public async Task<BookingDto?> Handle(GetBookingQuery query, CancellationToken ct)
    {
        var cacheKey = $"booking:{query.BookingId}";

        // Check cache first — avoids DB round-trip on repeated reads
        var cached = await cache.GetAsync<BookingDto>(cacheKey, ct);
        if (cached is not null) return cached;  // cache HIT: <1ms

        // Cache MISS: load from DB with related entities
        var booking = await uow.Bookings.GetByIdAsync(query.BookingId, ct);
        if (booking is null) return null;

        var customer = await uow.Customers.GetByIdAsync(booking.CustomerId, ct);
        var venue    = await uow.Venues.GetByIdAsync(booking.VenueId, ct);

        var dto = new BookingDto(
            booking.Id, booking.CustomerId, customer!.Name,
            booking.VenueId, venue!.Name, booking.SlotDate,
            booking.GuestCount, booking.Status, booking.TotalAmount, booking.CreatedAt);

        // Store in cache for 5 minutes
        await cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5), ct);
        return dto;
    }
}

/// <summary>
/// GET /api/bookings/customer/{customerId}
/// Returns all bookings for a customer, newest first.
/// Cached for 2 minutes (shorter TTL since new bookings invalidate this).
/// </summary>
public class GetCustomerBookingsHandler(IUnitOfWork uow, ICacheService cache)
    : IRequestHandler<GetCustomerBookingsQuery, IReadOnlyList<BookingDto>>
{
    public async Task<IReadOnlyList<BookingDto>> Handle(
        GetCustomerBookingsQuery query, CancellationToken ct)
    {
        var cacheKey = $"customer:{query.CustomerId}:bookings";
        var cached   = await cache.GetAsync<List<BookingDto>>(cacheKey, ct);
        if (cached is not null) return cached;

        var bookings = await uow.Bookings.GetByCustomerAsync(query.CustomerId, ct);
        var result   = new List<BookingDto>();

        foreach (var b in bookings)
        {
            var customer = await uow.Customers.GetByIdAsync(b.CustomerId, ct);
            var venue    = await uow.Venues.GetByIdAsync(b.VenueId, ct);
            result.Add(new(b.Id, b.CustomerId, customer!.Name, b.VenueId, venue!.Name,
                b.SlotDate, b.GuestCount, b.Status, b.TotalAmount, b.CreatedAt));
        }

        await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2), ct);
        return result;
    }
}

/// <summary>
/// GET /api/bookings/slots?venueId=...&amp;date=...
/// Returns available DateTime slots for a venue on a given date.
/// Generates slots every 2 hours from 9AM to 9PM, then removes booked ones.
/// Cached for 1 minute (short TTL since new bookings fill up slots fast).
/// </summary>
public class GetAvailableSlotsHandler(IUnitOfWork uow, ICacheService cache)
    : IRequestHandler<GetAvailableSlotsQuery, IReadOnlyList<DateTime>>
{
    public async Task<IReadOnlyList<DateTime>> Handle(
        GetAvailableSlotsQuery query, CancellationToken ct)
    {
        var cacheKey = $"slots:{query.VenueId}:{query.Date:yyyyMMdd}";
        var cached   = await cache.GetAsync<List<DateTime>>(cacheKey, ct);
        if (cached is not null) return cached;

        // Generate all possible slots: 9AM, 11AM, 1PM, 3PM, 5PM, 7PM, 9PM
        var allSlots = new[] { 9, 11, 13, 15, 17, 19, 21 }
            .Select(h => query.Date.Date.AddHours(h))
            .ToList();

        // Find which slots are already booked (non-cancelled)
        var existingBookings = await uow.Bookings
            .GetByVenueAndDateAsync(query.VenueId, query.Date, ct);

        var bookedTimes = existingBookings
            .Where(b => b.Status != BookingStatus.Cancelled)
            .Select(b => b.SlotDate)
            .ToHashSet();

        // Return only the slots not yet fully booked
        var available = allSlots.Where(s => !bookedTimes.Contains(s)).ToList();
        await cache.SetAsync(cacheKey, available, TimeSpan.FromMinutes(1), ct);
        return available;
    }
}
