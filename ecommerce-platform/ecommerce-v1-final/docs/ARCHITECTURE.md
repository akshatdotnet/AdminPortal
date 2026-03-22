# ECommerce Platform — Architecture & Developer Guide

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        API Gateway (Ocelot)                     │
│          Rate Limiting · JWT Validation · Load Balancing        │
└──────────┬──────────┬──────────┬──────────┬──────────┬──────────┘
           │          │          │          │          │
    ┌──────▼──┐ ┌─────▼───┐ ┌───▼──────┐ ┌▼────────┐ ┌▼─────────┐
    │Identity │ │ Product │ │  Order   │ │  Cart   │ │ Payment  │
    │   API   │ │   API   │ │   API    │ │   API   │ │   API    │
    └──────┬──┘ └─────┬───┘ └───┬──────┘ └────┬────┘ └────┬─────┘
           │          │          │              │           │
    ┌──────▼──────────▼──────────▼──────────────▼───────────▼─────┐
    │                    PostgreSQL + Redis + RabbitMQ             │
    └─────────────────────────────────────────────────────────────┘
```

## Clean Architecture Layers (per service)

```
Service/
├── Domain/               ← Entities, Value Objects, Domain Events
│   ├── Entities/         ← Aggregate roots with business logic
│   ├── Events/           ← Domain events (BaseDomainEvent records)
│   └── Interfaces/       ← Abstractions defined by domain
│
├── Application/          ← Use Cases (Commands & Queries)
│   ├── Commands/         ← State-changing operations (MediatR IRequest)
│   ├── Queries/          ← Read-only operations (return DTOs, not entities)
│   ├── DTOs/             ← Data transfer objects
│   ├── Validators/       ← FluentValidation validators
│   └── Behaviors/        ← Cross-cutting concerns (pipeline)
│
├── Infrastructure/       ← External dependencies (EF, Redis, SMTP)
│   ├── Persistence/      ← DbContext, Repositories, Configurations
│   └── Services/         ← External service implementations
│
└── API/                  ← HTTP Layer (thin controllers)
    ├── Controllers/      ← Maps HTTP ↔ MediatR commands/queries
    └── Middleware/       ← Exception handling, logging
```

## Design Principles Applied

### 1. SOLID Principles
- **S** — Each class has one responsibility (OrderController just routes to MediatR)
- **O** — Open for extension (new payment gateways via IPaymentGatewayService)
- **L** — Substitutable implementations (BCryptPasswordHasher : IPasswordHasher)
- **I** — Granular interfaces (IOrderRepository, IUnitOfWorkOrder)
- **D** — Controllers depend on IMediator, not concrete handlers

### 2. CQRS Pattern
- Commands mutate state (PlaceOrderCommand, CancelOrderCommand)
- Queries only read (GetOrderByIdQuery, GetProductsQuery)
- Read models use optimized DTOs, not domain entities

### 3. Domain Events
- Events raised inside aggregates (order.ConfirmPayment() → OrderConfirmedEvent)
- Dispatched after SaveChanges via MediatR.Publish
- Email service subscribes via INotificationHandler

### 4. Result Pattern (No Exception-Driven Flow)
- All command/query handlers return Result<T>
- Controllers map Result → HTTP status codes
- Only throw for truly exceptional errors (programmer mistakes)

### 5. Repository + Unit of Work
- Repositories abstract EF Core from Application layer
- UnitOfWork groups multiple operations atomically
- Domain events dispatched inside SaveChangesAsync

## Key Packages

| Package | Purpose |
|---|---|
| MediatR | CQRS — commands, queries, domain events |
| FluentValidation | Input validation with pipeline integration |
| Entity Framework Core | ORM with PostgreSQL via Npgsql |
| StackExchange.Redis | Shopping cart session store |
| BCrypt.Net-Next | Password hashing (cost factor 12) |
| SendGrid | Transactional email delivery |
| Stripe.net | Payment gateway integration |
| Ocelot | API Gateway with routing, rate limiting |
| Serilog | Structured logging |
| Polly | Resilience (retry, circuit breaker) |
| OpenTelemetry | Distributed tracing |

## Database Schema (per service)

- `identity_db` → users, addresses
- `product_db` → products, categories, images, reviews
- `order_db` → orders, order_items, order_status_history
- `coupon_db` → coupons
- `payment_db` → payment_records

## Environment Variables Reference

```bash
# Required for ALL services
JWT_SECRET_KEY=          # Min 32-char secret for HS256 signing
POSTGRES_PASSWORD=       # PostgreSQL password

# Payment service
STRIPE_SECRET_KEY=       # sk_test_ or sk_live_
STRIPE_WEBHOOK_SECRET=   # whsec_ from Stripe dashboard

# Email service
SENDGRID_API_KEY=        # SG. prefixed key

# Cart service
REDIS_PASSWORD=          # Redis AUTH password
```
