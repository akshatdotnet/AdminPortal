# 🏨 BookingSystem — .NET 8 Microservices Demo

A **production-grade** Booking + Order management system built with .NET 8.
Demonstrates Clean Architecture, CQRS, Domain Events, EF Core, and the full
**Booking → Order → Payment → Notification** flow.

---

## ⚡ Quick Start (5 minutes)

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- That's it. No Docker, no Redis, no RabbitMQ needed for local dev.

### Run on Windows
```bat
run.bat
```

### Run on Mac / Linux
```bash
chmod +x run.sh && ./run.sh
```

### Then open:
| URL | What it is |
|-----|------------|
| http://localhost:5000 | Swagger UI (try every endpoint here) |
| http://localhost:5000/health | Health check |
| http://localhost:5000/api/events | Live domain event log |
| http://localhost:5000/api/demo/full-flow | Run the entire flow in 1 click |

---

## 🏗️ Architecture

```
BookingSystem/
├── src/
│   ├── BookingSystem.Core/           ← Domain (Entities, Interfaces, CQRS)
│   │   ├── Entities/                 ← Customer, Venue, Booking, Order
│   │   ├── Interfaces/               ← IRepository, IUnitOfWork, IEventBus
│   │   ├── Events/                   ← Domain events (BookingCreated, OrderPaid…)
│   │   ├── Features/Bookings/        ← Commands, Queries, Handlers
│   │   └── Validators/               ← FluentValidation rules
│   │
│   ├── BookingSystem.Infrastructure/ ← EF Core, Repositories, Cache, EventBus
│   │   ├── Data/                     ← AppDbContext, Repositories, UnitOfWork
│   │   ├── Migrations/               ← EF Core SQLite migrations (pre-generated)
│   │   └── Services/                 ← MemoryCacheService, InMemoryEventBus
│   │
│   ├── BookingSystem.API/            ← ASP.NET Core Minimal API
│   │   ├── Program.cs                ← DI wiring, middleware, pipeline behaviours
│   │   └── Endpoints.cs              ← All HTTP routes
│   │
│   └── BookingSystem.Worker/         ← Background workers (Email, Analytics)
│       └── Workers/                  ← EmailNotificationWorker, AnalyticsWorker
│
├── docker/
│   ├── docker-compose.yml            ← Redis + RabbitMQ for production-like dev
│   └── Dockerfile.API
│
└── scripts/
    └── api-tests.http                ← VS Code REST Client test file
```

---

## 🔄 Complete Flow: Booking → Order → Payment → Notification

### Real-World Example: "Rahul books Grand Ballroom for his wedding"

```
1. CUSTOMER checks available slots
   GET /api/bookings/slots?venueId=...&date=2025-12-25
   → Returns: ["2025-12-25T09:00", "2025-12-25T11:00", ...]

2. CUSTOMER creates a booking
   POST /api/bookings
   Body: { customerId, venueId, slotDate, guestCount: 50 }
   
   What happens inside (CreateBookingHandler):
   ├── Validate inputs (FluentValidation pipeline)
   ├── Load Customer + Venue from DB
   ├── Check slot availability (Redis cache → DB)
   ├── Calculate price: 50 guests × ₹500 = ₹25,000
   ├── booking = Booking.Create(...)  ← domain entity
   ├── order   = Order.CreateFromBooking(booking) ← auto-created
   ├── uow.SaveChangesAsync() ← single transaction
   └── eventBus.Publish(BookingCreatedEvent) ← async, non-blocking
   
   Response: { bookingId, orderId, status: "Pending", amount: 25000 }

3. CUSTOMER pays the order
   POST /api/orders/{orderId}/pay
   Body: { cardToken: "valid_token_123" }
   
   What happens inside (ProcessPaymentHandler):
   ├── Load Order from DB
   ├── Call payment gateway (Razorpay/Stripe in production)
   ├── If SUCCESS:
   │   ├── order.MarkPaid("PAY_REF_ABC123")
   │   ├── booking.Confirm()  ← slot is now secured
   │   ├── uow.SaveChangesAsync()
   │   └── eventBus.Publish(OrderPaidEvent)
   └── If FAILED:
       ├── order.MarkFailed()
       └── eventBus.Publish(OrderFailedEvent)

4. ASYNC: EmailWorker consumes the events (simulated)
   BookingConfirmedEvent → "✅ Your booking is confirmed!"
   OrderPaidEvent        → "💳 Payment receipt: PAY_REF_ABC123"
   
5. ASYNC: AnalyticsWorker tracks metrics
   BookingCreatedEvent   → totalBookings++, totalRevenue += ₹25,000

6. CUSTOMER cancels (optional)
   POST /api/bookings/{bookingId}/cancel
   Body: { reason: "Change of plans" }
   
   What happens inside (CancelBookingHandler):
   ├── If order is Paid → order.Refund()
   ├── booking.Cancel(reason)
   └── eventBus.Publish(BookingCancelledEvent)
```

### Test payment failure:
```bash
# Create booking first, then pay with fail token:
POST /api/orders/{orderId}/pay
{ "cardToken": "fail_card_test" }
# → Order status: Failed, booking stays Pending
```

---

## 🧱 Design Patterns Used

### 1. Clean Architecture (Onion)
```
Core (domain) ← Infrastructure ← API
     ↑ no dependencies outward
```
- `Core` has zero external dependencies
- `Infrastructure` implements `Core` interfaces
- `API` wires everything together

### 2. CQRS with MediatR
```csharp
// Command (write) → triggers business logic + side effects
var booking = await mediator.Send(new CreateBookingCommand(...));

// Query (read) → cache-first, no side effects
var booking = await mediator.Send(new GetBookingQuery(id));
```

### 3. Repository + Unit of Work
```csharp
// All repos share one DbContext → one transaction
await uow.Bookings.AddAsync(booking);
await uow.Orders.AddAsync(order);
await uow.SaveChangesAsync(); // ← atomic commit
```

### 4. Domain Entity encapsulation
```csharp
// ✅ State changes go through the entity
booking.Confirm();    // validates state machine
booking.Cancel(reason); // enforces business rules

// ❌ Never do this
booking.Status = BookingStatus.Confirmed; // bypasses rules
```

### 5. Pipeline Behaviours (AOP)
```
Request → ValidationBehaviour → LoggingBehaviour → Handler → Response
            (FluentValidation)    (Stopwatch log)
```

### 6. Cache-Aside Pattern
```csharp
// Check cache first → on miss, load DB → write to cache
var cached = await cache.GetAsync<BookingDto>(key);
if (cached is not null) return cached;     // ← cache hit: <1ms

var data = await repo.GetAsync(...);       // ← cache miss: ~10ms
await cache.SetAsync(key, data, ttl: 5min);
return data;
```

### 7. Domain Events (async fire-and-forget)
```
HTTP Request ─┬─ CreateBooking ─ SaveDB ─ Publish Event ─► Response (fast)
              └─────────────────────────────────────────►  EmailWorker (async)
                                                        ►  AnalyticsWorker (async)
```

---

## 📡 API Reference

| Method | URL | Description |
|--------|-----|-------------|
| GET | `/health` | Health check |
| GET | `/api/venues` | List all venues |
| GET | `/api/venues/customers` | List seeded customers |
| GET | `/api/bookings/slots` | Available time slots |
| POST | `/api/bookings` | **Create booking** (+ auto-creates order) |
| GET | `/api/bookings/{id}` | Get booking |
| GET | `/api/bookings/customer/{id}` | Customer's bookings |
| POST | `/api/bookings/{id}/confirm` | Confirm booking |
| POST | `/api/bookings/{id}/cancel` | Cancel booking |
| GET | `/api/orders/{id}` | Get order |
| POST | `/api/orders/{id}/pay` | **Process payment** |
| POST | `/api/orders/{id}/refund` | Refund order |
| GET | `/api/events` | Domain event log |
| POST | `/api/demo/full-flow` | **Run full flow in 1 call** |

---

## 🔑 Seeded Test Data

| Type | Name | ID |
|------|------|----|
| Customer | Rahul Sharma | `00000000-0000-0000-0000-000000000001` |
| Customer | Priya Patel  | `00000000-0000-0000-0000-000000000002` |
| Venue | Grand Ballroom Mumbai (cap 300) | `00000000-0000-0000-0000-000000000011` |
| Venue | Sunset Terrace Pune (cap 150) | `00000000-0000-0000-0000-000000000012` |

---

## 🚀 Production Upgrades

To go from this local demo to production-ready:

| Feature | Local (this project) | Production |
|---------|---------------------|------------|
| Database | SQLite (file) | PostgreSQL / Azure SQL |
| Cache | IMemoryCache | Redis (StackExchange.Redis) |
| Message Bus | In-memory | RabbitMQ / Azure Service Bus |
| Auth | None | JWT Bearer / Azure AD |
| Containerisation | None | Docker + Kubernetes |
| Logging | Serilog Console | Serilog → Seq / ELK |
| Metrics | None | OpenTelemetry → Prometheus |
| Payment | Simulated | Razorpay / Stripe SDK |

---

## 🛠️ EF Core Migrations

If you modify the domain entities:
```bash
cd src/BookingSystem.API

# Create new migration
dotnet ef migrations add YourMigrationName \
  --project ../BookingSystem.Infrastructure \
  --startup-project .

# Apply to database
dotnet ef database update \
  --project ../BookingSystem.Infrastructure \
  --startup-project .
```

---

## 📦 Key NuGet Packages

| Package | Purpose |
|---------|---------|
| `MediatR` | CQRS command/query dispatch |
| `FluentValidation` | Input validation with rich error messages |
| `Microsoft.EntityFrameworkCore.Sqlite` | ORM + SQLite for local dev |
| `Swashbuckle.AspNetCore` | Swagger/OpenAPI docs |
| `Serilog.AspNetCore` | Structured logging |
| `StackExchange.Redis` | Redis client (when upgrading) |
| `RabbitMQ.Client` | Message bus (when upgrading) |
