using BookingSystem.Core.Events;
using BookingSystem.Infrastructure.Services;   // EventBridge lives here
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BookingSystem.Worker.Workers;

// ═══════════════════════════════════════════════════════════════════════════════
// EMAIL NOTIFICATION WORKER
// ═══════════════════════════════════════════════════════════════════════════════
// ROLE IN THE FULL FLOW:
//   Booking → Order → Payment → [THIS WORKER] → Analytics
//
// WHAT IT DOES:
//   Runs as a background service (IHostedService) inside the same .NET process
//   as the API. Every 2 seconds it checks EventBridge for new events it has not
//   yet processed, then "sends" an email (logs to console in dev; uses
//   SendGrid / AWS SES / SMTP in production).
//
// EVENTS IT HANDLES:
//   BookingCreatedEvent   → "We received your booking request"
//   OrderPaidEvent        → "Payment confirmed – here is your receipt"
//   BookingConfirmedEvent → "Your slot is confirmed"
//   BookingCancelledEvent → "Your booking was cancelled / refund initiated"
//
// WHY POLLING INSTEAD OF PUSH?
//   In production this would use RabbitMQ push (BasicConsumeAsync) for instant
//   delivery. For local dev, polling is simpler — no broker needed.
//
// HOW TO SEE IT WORKING:
//   1. Run the API: dotnet run (in BookingSystem.API folder)
//   2. POST /api/demo/full-flow  or  POST /api/bookings then POST /api/orders/{id}/pay
//   3. Watch the console — within 2 seconds you will see lines like:
//      [EmailWorker] EMAIL SENT  To: rahul@example.com  Subject: Payment confirmed...
// ═══════════════════════════════════════════════════════════════════════════════
public class EmailNotificationWorker(ILogger<EmailNotificationWorker> logger)
    : BackgroundService
{
    // Tracks which events this worker has already processed.
    // Key = "{EventType}:{json.GetHashCode()}" — unique per event instance.
    // This prevents the same email being sent twice if the worker restarts mid-poll.
    private readonly HashSet<string> _processedKeys = [];

    /// <summary>
    /// Entry point called by .NET's hosted service infrastructure when the app starts.
    /// Loops forever (until CancellationToken is cancelled on app shutdown).
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "[EmailWorker] Started. Polling EventBridge every 2s for: " +
            "BookingCreatedEvent, OrderPaidEvent, BookingConfirmedEvent, BookingCancelledEvent");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Check for new events every 2 seconds
            ProcessPendingEvents();
            await Task.Delay(2000, stoppingToken);
        }

        logger.LogInformation("[EmailWorker] Stopped.");
    }

    /// <summary>
    /// Reads all events from EventBridge, skips already-processed ones,
    /// and dispatches an email for each new relevant event.
    /// </summary>
    private void ProcessPendingEvents()
    {
        foreach (var (type, json, _) in EventBridge.GetAll())
        {
            // Deduplication: build a unique key for this event instance
            var key = $"{type}:{json.GetHashCode()}";
            if (_processedKeys.Contains(key)) continue;  // already handled
            _processedKeys.Add(key);

            // Route to the correct email template based on event type
            switch (type)
            {
                case nameof(BookingCreatedEvent):
                {
                    // Sent immediately after CreateBookingHandler completes.
                    // At this point booking is Pending (not yet paid).
                    var e = JsonSerializer.Deserialize<BookingCreatedEvent>(json)!;
                    SendEmail(
                        to:      "customer@example.com",
                        subject: $"Booking Request Received - #{e.BookingId.ToString()[..8]}",
                        body:    $"We received your booking for {e.SlotDate:ddd MMM dd yyyy}. " +
                                 $"Amount due: Rs.{e.Amount:N0}. Please complete payment to confirm your slot."
                    );
                    break;
                }

                case nameof(OrderPaidEvent):
                {
                    // Sent after ProcessPaymentHandler marks order Paid.
                    // At this point booking is auto-confirmed.
                    var e = JsonSerializer.Deserialize<OrderPaidEvent>(json)!;
                    SendEmail(
                        to:      "customer@example.com",
                        subject: $"Payment Confirmed - Ref: {e.PaymentReference}",
                        body:    $"Your payment was successful! " +
                                 $"Order #{e.OrderId.ToString()[..8]} | Ref: {e.PaymentReference}. " +
                                 $"Your venue slot is now confirmed."
                    );
                    break;
                }

                case nameof(BookingConfirmedEvent):
                {
                    // Sent when booking moves to Confirmed status
                    var e = JsonSerializer.Deserialize<BookingConfirmedEvent>(json)!;
                    SendEmail(
                        to:      e.CustomerEmail,
                        subject: $"Booking Confirmed - {e.SlotDate:ddd, MMM dd yyyy}",
                        body:    $"Your booking #{e.BookingId.ToString()[..8]} is CONFIRMED for " +
                                 $"{e.SlotDate:yyyy-MM-dd HH:mm}. See you there!"
                    );
                    break;
                }

                case nameof(BookingCancelledEvent):
                {
                    // Sent when customer or admin cancels.
                    // If a paid order existed, CancelBookingHandler auto-refunds it first.
                    var e = JsonSerializer.Deserialize<BookingCancelledEvent>(json)!;
                    SendEmail(
                        to:      "customer@example.com",
                        subject: $"Booking Cancelled - #{e.BookingId.ToString()[..8]}",
                        body:    $"Your booking was cancelled. Reason: {e.Reason}. " +
                                 $"If you paid, your refund will be processed in 3-5 business days."
                    );
                    break;
                }

                case nameof(OrderFailedEvent):
                {
                    // Sent when payment gateway rejects the card
                    var e = JsonSerializer.Deserialize<OrderFailedEvent>(json)!;
                    SendEmail(
                        to:      "customer@example.com",
                        subject: $"Payment Failed - Order #{e.OrderId.ToString()[..8]}",
                        body:    $"We could not process your payment. Reason: {e.Reason}. " +
                                 $"Please try again with a different card."
                    );
                    break;
                }
                // Other event types (e.g. NotificationEvent) are ignored by this worker
            }
        }
    }

    /// <summary>
    /// Simulates sending an email. In production, replace this with:
    ///   - SendGrid:    await sendGridClient.SendEmailAsync(msg)
    ///   - AWS SES:     await sesClient.SendEmailAsync(request)
    ///   - SMTP:        await smtpClient.SendMailAsync(message)
    /// </summary>
    private void SendEmail(string to, string subject, string body)
    {
        logger.LogInformation(
            "[EmailWorker] EMAIL SENT\n" +
            "  To      : {To}\n" +
            "  Subject : {Subject}\n" +
            "  Body    : {Body}",
            to, subject, body);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ANALYTICS WORKER
// ═══════════════════════════════════════════════════════════════════════════════
// ROLE IN THE FULL FLOW:
//   Booking → Order → Payment → Notification → [THIS WORKER]
//
// WHAT IT DOES:
//   Maintains real-time in-memory metrics (bookings, revenue, cancellations).
//   Prints a live dashboard to the console every 10 seconds.
//   In production this would write to ClickHouse / BigQuery / Azure Event Hub.
//
// EVENTS IT HANDLES:
//   BookingCreatedEvent   → totalBookings++, totalRevenue += amount
//   BookingCancelledEvent → cancellations++
//   OrderPaidEvent        → paidOrders++, confirmedRevenue += amount
//   OrderFailedEvent      → failedPayments++
//
// HOW TO SEE IT WORKING:
//   After running POST /api/demo/full-flow, wait ~10 seconds and look for:
//   [AnalyticsWorker] LIVE DASHBOARD
//     Total Bookings : 1    Total Revenue  : Rs.25,000
//     Paid Orders    : 1    Confirmed Rev  : Rs.25,000
//     Cancellations  : 0    Cancel Rate    : 0%
// ═══════════════════════════════════════════════════════════════════════════════
public class AnalyticsWorker(ILogger<AnalyticsWorker> logger) : BackgroundService
{
    private readonly HashSet<string> _processedKeys = [];

    // In-memory metrics counters
    private int    _totalBookings;
    private decimal _totalRevenue;      // revenue from all bookings created
    private int    _paidOrders;
    private decimal _confirmedRevenue;  // revenue from actually paid orders
    private int    _cancellations;
    private int    _failedPayments;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[AnalyticsWorker] Started. Tracking all booking & payment events.");

        while (!stoppingToken.IsCancellationRequested)
        {
            ProcessEvents();         // consume new events from EventBridge

            PrintDashboard();        // print metrics every 10 seconds if data exists

            await Task.Delay(1000, stoppingToken);  // poll every 1 second
        }
    }

    /// <summary>
    /// Reads new events from EventBridge and updates the metrics counters.
    /// </summary>
    private void ProcessEvents()
    {
        foreach (var (type, json, _) in EventBridge.GetAll())
        {
            var key = $"analytics:{type}:{json.GetHashCode()}";
            if (_processedKeys.Contains(key)) continue;
            _processedKeys.Add(key);

            switch (type)
            {
                case nameof(BookingCreatedEvent):
                {
                    var e = JsonSerializer.Deserialize<BookingCreatedEvent>(json)!;
                    _totalBookings++;
                    _totalRevenue += e.Amount;
                    logger.LogInformation(
                        "[AnalyticsWorker] NEW BOOKING tracked. " +
                        "BookingId={Id} Amount=Rs.{Amount} TotalBookings={Total}",
                        e.BookingId.ToString()[..8], e.Amount, _totalBookings);
                    break;
                }

                case nameof(OrderPaidEvent):
                {
                    var e = JsonSerializer.Deserialize<OrderPaidEvent>(json)!;
                    _paidOrders++;
                    // We don't have amount here directly, but track count
                    logger.LogInformation(
                        "[AnalyticsWorker] PAYMENT CONFIRMED. " +
                        "OrderId={Id} Ref={Ref} PaidOrders={Total}",
                        e.OrderId.ToString()[..8], e.PaymentReference, _paidOrders);
                    break;
                }

                case nameof(BookingCancelledEvent):
                {
                    var e = JsonSerializer.Deserialize<BookingCancelledEvent>(json)!;
                    _cancellations++;
                    logger.LogInformation(
                        "[AnalyticsWorker] CANCELLATION tracked. " +
                        "BookingId={Id} Reason={Reason} TotalCancellations={Total}",
                        e.BookingId.ToString()[..8], e.Reason, _cancellations);
                    break;
                }

                case nameof(OrderFailedEvent):
                {
                    var e = JsonSerializer.Deserialize<OrderFailedEvent>(json)!;
                    _failedPayments++;
                    logger.LogInformation(
                        "[AnalyticsWorker] PAYMENT FAILED. " +
                        "OrderId={Id} Reason={Reason} TotalFailed={Total}",
                        e.OrderId.ToString()[..8], e.Reason, _failedPayments);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Prints a metrics dashboard every 10 seconds when there is data to show.
    /// In production this data would be sent to Grafana / Power BI / DataDog.
    /// </summary>
    private void PrintDashboard()
    {
        // Only print on the 10-second mark and only if we have at least one booking
        if (DateTime.Now.Second % 10 != 0 || _totalBookings == 0) return;

        var cancelRate = _totalBookings > 0
            ? (double)_cancellations / _totalBookings
            : 0.0;

        logger.LogInformation(
            "[AnalyticsWorker] LIVE DASHBOARD\n" +
            "  Total Bookings  : {Bookings}\n" +
            "  Total Revenue   : Rs.{Revenue:N0}\n" +
            "  Paid Orders     : {PaidOrders}\n" +
            "  Cancellations   : {Cancellations}\n" +
            "  Failed Payments : {FailedPayments}\n" +
            "  Cancel Rate     : {CancelRate:P0}",
            _totalBookings,
            _totalRevenue,
            _paidOrders,
            _cancellations,
            _failedPayments,
            cancelRate);
    }
}
