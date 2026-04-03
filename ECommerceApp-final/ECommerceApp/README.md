# 🛒 ECommerce Clean Architecture — .NET 8 Console App

A fully-featured e-commerce platform built with **Clean Architecture**, **SOLID principles**,
**Domain-Driven Design (DDD)**, and **CQRS via MediatR**.

---

## ✅ Prerequisites

| Tool       | Version  | Download                                  |
|------------|----------|-------------------------------------------|
| .NET SDK   | **8.0+** | https://dotnet.microsoft.com/download     |

Check your version:
```bash
dotnet --version   # must print 8.0.x
```

No SQL Server required — uses **SQLite** (zero-config, file-based, auto-created).

---

## 🚀 Quick Start

```bash
# 1. Go to console project
cd src/ECommerce.Console

# 2. Run interactive menu
dotnet run

# 3. OR run full automated demo (no input needed — tests all 6 flows)
dotnet run -- --demo

# 4. Fresh database + demo (use this if you want a clean run)
dotnet run -- --demo --reset
```

On first run the app will:
1. Create `ecommerce.db` (SQLite) automatically
2. Seed **13 products**, **4 categories**, **3 demo customers**
3. Either open the interactive menu or run the demo flow

---

## 🎮 Interactive Menu

```
  [1]  🛍️  Browse Products          → See all 13 products with price & stock
  [2]  🔍  Search Products           → Full-text search by name/description
  [3]  ➕  Add to Cart               → Pick product + quantity → added to cart
  [4]  ➖  Remove from Cart          → Remove an item from your cart
  [5]  🛒  View My Cart              → See items, quantities, running total
  [6]  📦  Place Order               → Confirm cart, enter address, create order
  [7]  📋  My Orders                 → List all your orders with status
  [8]  💳  Pay for Order             → Choose payment method, process payment
  [9]  ❌  Cancel Order              → Cancel a Pending/Confirmed order
  [R]  💸  Request Refund            → Initiate refund on a cancelled order
  [D]  🚀  Run Full Demo Flow        → Automated end-to-end check (all flows)
  [S]  👤  Switch Customer           → Switch between 3 demo users
  [0]  🚪  Exit
```

### Recommended Manual Test Path
```
[1] Browse Products        → see the catalogue
[3] Add to Cart            → pick product #1, qty 2
[3] Add to Cart            → pick product #5, qty 1
[5] View Cart              → verify items and total
[6] Place Order            → enter address → confirm
[7] My Orders              → see status = Pending
[8] Pay for Order          → select COD (always succeeds)
[7] My Orders              → status = Confirmed

--- Test cancel + refund ---
[3] Add to Cart            → add another item
[6] Place Order            → place a second order
[9] Cancel Order           → cancel the new order
[R] Request Refund         → refund the cancelled order
```

---

## 🚀 One-Command Full Flow Demo

```bash
dotnet run -- --demo --reset
```

This single command will:

1. **Wipe and re-seed** the database (fresh state every time)
2. **Auto-select** the first demo customer (no input needed)
3. **Run all 6 sections** end-to-end and print ✅/❌ for each check:

```
══════════════════════════════════════════════════════════════════
  SECTION 1 — PRODUCT LISTING
══════════════════════════════════════════════════════════════════
    ✅ PASS  ListProductsQuery
    ✅ PASS  Products seeded (>0 products)
    ℹ  Products found: 13
    ✅ PASS  Search 'iPhone'
    ✅ PASS  GetProductByIdQuery (iPhone 15 Pro)

══════════════════════════════════════════════════════════════════
  SECTION 2 — ADD TO CART
══════════════════════════════════════════════════════════════════
    ✅ PASS  Add 'iPhone 15 Pro' × 2 to cart
    ✅ PASS  Add 'Sony WH-1000XM5' × 1 to cart
    ✅ PASS  Cart has 2 distinct items
    ✅ PASS  Quantities merged to 3
    ✅ PASS  Cart has 1 item after removal
    ...

══════════════════════════════════════════════════════════════════
  SECTION 3 — PLACE ORDER
══════════════════════════════════════════════════════════════════
    ✅ PASS  PlaceOrderCommand
    ✅ PASS  Order status = Pending
    ✅ PASS  Cart cleared after order placed
    ...

══════════════════════════════════════════════════════════════════
  SECTION 4 — PAYMENT
══════════════════════════════════════════════════════════════════
    ✅ PASS  ProcessPaymentCommand (COD)
    ✅ PASS  Payment status = Captured
    ✅ PASS  Order status updated to Confirmed
    ✅ PASS  Duplicate payment rejected (idempotency guard)
    ...

══════════════════════════════════════════════════════════════════
  SECTION 5 — CANCEL & REFUND
══════════════════════════════════════════════════════════════════
    ✅ PASS  CancelOrderCommand
    ✅ PASS  Order status = Cancelled
    ✅ PASS  Second cancellation rejected by domain rule
    ✅ PASS  RefundPaymentCommand on paid+cancelled order
    ✅ PASS  Payment status = Refunded
    ...

══════════════════════════════════════════════════════════════════
  SECTION 6 — DOMAIN RULES & EDGE CASES
══════════════════════════════════════════════════════════════════
    ✅ PASS  Add non-existent product fails
    ✅ PASS  Zero quantity rejected
    ✅ PASS  Place order with empty cart fails
    ✅ PASS  Paying already-confirmed order is rejected
    ✅ PASS  Wrong customer cannot cancel another's order
    ...

══════════════════════════════════════════════════════════════════
  ✅ Passed: 28    ❌ Failed: 0    Total: 28
  🎉  ALL CHECKS PASSED
══════════════════════════════════════════════════════════════════
```

---

## 🗂️ Project Structure

```
ECommerceApp/
├── ECommerceApp.sln
└── src/
    ├── ECommerce.Domain/              ← Core — zero external dependencies
    │   ├── Entities/                  │   Order, Cart, Product, Payment, Customer
    │   ├── ValueObjects/              │   Money, Address, Email
    │   ├── Events/                    │   OrderPlaced, OrderCancelled, etc.
    │   ├── Exceptions/                │   DomainException
    │   ├── Interfaces/                │   IOrderRepository, IProductRepository, etc.
    │   └── Enums/                     │   OrderStatus, PaymentStatus, etc.
    │
    ├── ECommerce.Application/         ← Use cases (depends on Domain only)
    │   ├── Products/Queries/          │   ListProductsQuery, GetProductByIdQuery
    │   ├── Cart/Commands/             │   AddToCartCommand, RemoveFromCartCommand
    │   ├── Orders/Commands/           │   PlaceOrderCommand, CancelOrderCommand
    │   ├── Orders/Queries/            │   GetOrdersQuery
    │   ├── Payment/Commands/          │   ProcessPaymentCommand, RefundPaymentCommand
    │   └── Common/
    │       ├── Behaviours/            │   LoggingBehaviour, ExceptionHandlingBehaviour
    │       ├── Interfaces/            │   IUnitOfWork, IPaymentGateway, INotificationService
    │       └── Models/                │   Result<T>, DTOs
    │
    ├── ECommerce.Infrastructure/      ← Adapters (EF Core, gateways, services)
    │   ├── Persistence/
    │   │   ├── AppDbContext.cs        │   EF Core DbContext
    │   │   ├── UnitOfWork.cs          │   Commits all changes in one transaction
    │   │   ├── DataSeeder.cs          │   Seeds demo products and customers
    │   │   ├── Repositories/          │   Concrete: OrderRepository, CartRepository, etc.
    │   │   └── Configurations/        │   EF entity type configs (owned types, precision)
    │   └── Services/
    │       ├── Payment/               │   SimulatedPaymentGateway (90% success, COD=100%)
    │       └── Notification/          │   Console email + SMS (swap for SendGrid/Twilio)
    │
    └── ECommerce.Console/             ← Presentation (entry point)
        ├── Program.cs                 │   Host setup, --demo / --reset flags
        ├── EcommerceApp.cs            │   Interactive menu loop
        ├── Handlers/
        │   ├── ProductHandler.cs      │   Browse, Search
        │   ├── CartHandler.cs         │   Add, Remove, View
        │   ├── OrderHandler.cs        │   Place, View, Cancel
        │   ├── PaymentHandler.cs      │   Pay, Refund
        │   └── DemoFlowRunner.cs      │   Automated end-to-end validator
        └── Services/
            └── ConsoleDisplayService  │   All console rendering (SRP)
```

---

## 🏗️ Architecture Principles Applied

### Layer Dependencies (Dependency Rule)
```
Console ──► Application ──► Domain
Infrastructure ──────────► Application, Domain
```

### SOLID in This Codebase

| Letter | Principle | Example |
|--------|-----------|---------|
| **S** | Single Responsibility | `PlaceOrderCommandHandler` only places orders. `ConsoleDisplayService` only renders. |
| **O** | Open/Closed | Add Razorpay: implement `IPaymentGateway` → zero existing code changes |
| **L** | Liskov Substitution | `ConsoleEmailNotificationService` and future `SendGridService` are interchangeable |
| **I** | Interface Segregation | `IOrderRepository` and `ICartRepository` are separate — cart doesn't expose order methods |
| **D** | Dependency Inversion | Application depends on `IOrderRepository` (abstract); never EF Core directly |

### Key Patterns
- **CQRS** — Commands mutate state; Queries read state; they never mix
- **MediatR** — All operations dispatched through a typed request/handler pipeline
- **Repository + Unit of Work** — All data access behind interfaces; committed in one transaction
- **Domain Events** — `Order.Create()` raises `OrderPlacedEvent`; infrastructure reacts after persistence
- **Result\<T\>** — No exceptions for expected failures; callers get `Result.Failure("message")`
- **Value Objects** — `Money`, `Address`, `Email` are immutable; equality by value, not reference
- **Aggregate Roots** — `Order`, `Cart`, `Product`, `Payment` own and enforce their invariants

---

## ⚙️ Configuration

### Switch to SQL Server
In `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=ECommerceDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

In `InfrastructureExtensions.cs`, replace:
```csharp
opt.UseSqlite(...)
// with:
opt.UseSqlServer(config.GetConnectionString("DefaultConnection"))
```

Add package:
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.8
```

### Real Payment Gateway (Stripe)
1. `dotnet add package Stripe.net`
2. Create `StripePaymentGateway : IPaymentGateway`
3. Change registration in `InfrastructureExtensions.cs` — zero application code changes (OCP ✅)

### Real Email (SendGrid)
1. `dotnet add package SendGrid`
2. Create `SendGridEmailService : IEmailNotificationService`
3. Swap registration — zero application code changes (OCP ✅)

---

## 🔧 Common Issues

| Issue | Fix |
|-------|-----|
| `dotnet: command not found` | Install .NET 8 SDK from https://dotnet.microsoft.com/download |
| Build errors about packages | Run `dotnet restore` from project root |
| `ecommerce.db locked` | Close DB Browser or other tools; or use `--reset` |
| Payment keeps failing | Use COD (option 5 in payment menu) — always succeeds |
| Demo shows failures | Run `dotnet run -- --demo --reset` for a clean state |

---

## 📦 Packages Used

| Package | Version | Why |
|---------|---------|-----|
| `MediatR` | 12.3.0 | CQRS dispatcher + pipeline behaviours |
| `Microsoft.EntityFrameworkCore.Sqlite` | 8.0.8 | Zero-config local database |
| `Microsoft.Extensions.Hosting` | 8.0.0 | Generic Host, DI, configuration |
| `Microsoft.Extensions.Logging.Console` | 8.0.0 | Structured console logging |

