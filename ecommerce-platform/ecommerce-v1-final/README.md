# ShopHub E-Commerce Platform

## Architecture

Clean Architecture + CQRS + Domain-Driven Design — 7 microservices, all runnable locally.

### Services

| Service | Port | Database |
|---|---|---|
| Identity API | 5001 | PostgreSQL (identity_db) |
| Product API | 5002 | PostgreSQL (product_db) |
| Order API | 5003 | PostgreSQL (order_db) |
| Cart API | 5004 | Redis |
| Coupon API | 5005 | PostgreSQL (coupon_db) |
| Payment API | 5006 | PostgreSQL (payment_db) |
| Email Service | 5007 | — (SendGrid / console log) |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- (Optional) [Stripe CLI](https://stripe.com/docs/stripe-cli) for payment testing

## Quick Start

### 1. Start Infrastructure

```bash
docker compose up -d postgres redis
```

Wait ~10 seconds for PostgreSQL to initialize and create all databases.

### 2. Restore & Build

```bash
dotnet restore
dotnet build
```

### 3. Run All Services

```bash
chmod +x scripts/run-all.sh
./scripts/run-all.sh
```

Or start services individually:

```bash
# Terminal 1
cd src/Services/Identity/Identity.Api && dotnet run

# Terminal 2
cd src/Services/ProductAPI/Product.Api && dotnet run

# Terminal 3
cd src/Services/OrderAPI/Order.Api && dotnet run

# Terminal 4
cd src/Services/ShoppingCartAPI/Cart.Api && dotnet run

# Terminal 5
cd src/Services/CouponAPI/Coupon.Api && dotnet run

# Terminal 6
cd src/Services/PaymentAPI/Payment.Api && dotnet run

# Terminal 7
cd src/Services/EmailService/Email.Api && dotnet run
```

### 4. Run the E2E Test

```bash
chmod +x scripts/test-e2e.sh
./scripts/test-e2e.sh
```

### 5. Run Unit Tests

```bash
dotnet test tests/Identity.Tests
dotnet test tests/Order.Tests
```

## Swagger UIs

- Identity: http://localhost:5001/swagger
- Product: http://localhost:5002/swagger
- Orders: http://localhost:5003/swagger
- Cart: http://localhost:5004/swagger
- Coupons: http://localhost:5005/swagger
- Payments: http://localhost:5006/swagger
- Email: http://localhost:5007/swagger

## Key Design Patterns

- **CQRS**: Commands (write) and Queries (read) separated via MediatR
- **Domain Events**: Raised inside aggregates, dispatched after SaveChanges
- **Result Pattern**: No exception-driven flow for business errors
- **Repository + Unit of Work**: Clean separation between domain and data access
- **Pipeline Behaviors**: Validation, logging, performance monitoring via MediatR pipeline
- **Clean Architecture**: Domain → Application → Infrastructure → API (dependencies always point inward)

## Configuration

All services share the same JWT secret key. Update `appsettings.json` in each service:

```json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "ShopHub",
    "Audience": "ShopHub-API",
    "ExpiryMinutes": 60
  }
}
```

Database connections default to:
- Host: `localhost:5432`
- Username: `ecommerce`
- Password: `ecommerce_secret`
