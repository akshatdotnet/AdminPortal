using BookingSystem.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BookingSystem.Infrastructure.Services;

// ═══════════════════════════════════════════════════════════════════════════════
// MEMORY CACHE SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
// PURPOSE : Implements the Cache-Aside pattern using .NET built-in IMemoryCache.
//
// FLOW (Cache-Aside):
//   Request comes in → GetAsync(key)
//   ├── Cache HIT  → return from RAM in <1ms  (no database call)
//   └── Cache MISS → caller queries DB → SetAsync(key, value) → return value
//
// KEY NAMES USED IN THIS APP:
//   "booking:{guid}"              → single booking DTO,   TTL 5 min
//   "customer:{guid}:bookings"    → customer's list,      TTL 2 min
//   "slots:{venueId}:{yyyyMMdd}"  → available time slots, TTL 1 min
//
// PRODUCTION UPGRADE:
//   Swap this for RedisCacheService. Only the constructor changes — all
//   ICacheService callers (handlers) stay identical.
// ═══════════════════════════════════════════════════════════════════════════════
public class MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var hit = cache.TryGetValue(key, out string? json);
        logger.LogDebug("Cache {Result}: {Key}", hit ? "HIT" : "MISS", key);
        if (!hit || json is null) return Task.FromResult<T?>(null);
        return Task.FromResult(JsonSerializer.Deserialize<T>(json));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? TimeSpan.FromMinutes(5)
        };
        cache.Set(key, JsonSerializer.Serialize(value), options);
        logger.LogDebug("Cache SET: {Key} TTL={TTL}", key, ttl);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// EVENT BRIDGE  — shared in-process bus connecting API → Workers
// ═══════════════════════════════════════════════════════════════════════════════
// WHY THIS EXISTS:
//   InMemoryEventBus (in API) and the Background Workers run in the same .NET
//   process when you do "dotnet run". Static state is shared across the whole
//   AppDomain, so this static class acts as the message queue.
//
// COMPLETE FLOW:
//   [API Thread]
//   CreateBookingHandler.Handle()
//     └─► eventBus.PublishAsync(new BookingCreatedEvent(...))
//           └─► InMemoryEventBus.PublishAsync()
//                 └─► EventBridge.Append("BookingCreatedEvent", json)  ← WRITES HERE
//
//   [Background Thread — every 2 seconds]
//   EmailNotificationWorker.ProcessPendingEvents()
//     └─► EventBridge.GetAll()   ← READS HERE
//           └─► Sees "BookingCreatedEvent" → sends confirmation email log
//
//   [Background Thread — every 1 second]
//   AnalyticsWorker.ProcessEvents()
//     └─► EventBridge.GetAll()   ← READS HERE
//           └─► Sees "BookingCreatedEvent" → increments totalBookings, totalRevenue
//
// PRODUCTION REPLACEMENT:
//   This entire class is replaced by RabbitMQ / Azure Service Bus:
//   Publish:  channel.BasicPublishAsync(exchange:"bookings", routingKey:eventType, body:json)
//   Consume:  channel.BasicConsumeAsync(queue:"booking.email", consumer:emailConsumer)
// ═══════════════════════════════════════════════════════════════════════════════
public static class EventBridge
{
    private static readonly List<(string Type, string Json, DateTime At)> _events = [];
    private static readonly object _lock = new();

    /// <summary>Called by InMemoryEventBus after every PublishAsync.</summary>
    public static void Append(string type, string json)
    {
        lock (_lock)
        {
            _events.Add((type, json, DateTime.UtcNow));
            if (_events.Count > 200) _events.RemoveAt(0); // rolling window
        }
    }

    /// <summary>Called by workers to poll for new events. Returns a snapshot copy.</summary>
    public static IReadOnlyList<(string Type, string Json, DateTime At)> GetAll()
    {
        lock (_lock) return [.. _events];
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// IN-MEMORY EVENT BUS  (implements Core.Interfaces.IEventBus)
// ═══════════════════════════════════════════════════════════════════════════════
// PURPOSE : Called inside MediatR handlers to publish domain events.
//           The HTTP response is returned to the client BEFORE workers process
//           the event — this is the async decoupling pattern.
//
// EVENTS PUBLISHED IN THIS APP:
//   BookingCreatedEvent   → fired in CreateBookingHandler   after DB insert
//   BookingConfirmedEvent → fired in ConfirmBookingHandler  after status change
//   BookingCancelledEvent → fired in CancelBookingHandler   after cancellation
//   OrderPaidEvent        → fired in ProcessPaymentHandler  after payment success
//   OrderFailedEvent      → fired in ProcessPaymentHandler  after payment failure
// ═══════════════════════════════════════════════════════════════════════════════
public class InMemoryEventBus(ILogger<InMemoryEventBus> logger) : IEventBus
{
    // Separate log just for the GET /api/events endpoint (shows in Swagger)
    private static readonly List<(string Type, string Data, DateTime At)> _log = [];
    public static IReadOnlyList<(string Type, string Data, DateTime At)> EventLog => _log.AsReadOnly();

    public Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : class
    {
        var json = JsonSerializer.Serialize(@event);

        // 1. Store in the API event log (visible at GET /api/events)
        _log.Add((typeof(T).Name, json, DateTime.UtcNow));
        if (_log.Count > 100) _log.RemoveAt(0);

        // 2. Feed EventBridge so background workers (Email, Analytics) see this event
        EventBridge.Append(typeof(T).Name, json);

        // 3. Log to console — you will see this line in the terminal for every event
        logger.LogInformation(
            "EVENT PUBLISHED [{EventType}]: {Payload}",
            typeof(T).Name, json);

        return Task.CompletedTask;
    }
}
