# AdminPortal - ASP.NET Core MVC Clean Architecture

A fully structured **Admin Portal** (inspired by Dukaan/vaishno.com store UI) built with
**ASP.NET Core 8 MVC**, **Clean Architecture**, and **SOLID principles**.
All data is currently served from in-memory mock stores — swap to real API clients with zero impact on business logic.

---

## Solution Structure

```
AdminPortal/
├── AdminPortal.sln
│
├── AdminPortal.Domain/              ← Enterprise Business Rules
│   ├── Entities/                    ← Store, Product, Order, Discount, Payout, StaffAccount...
│   ├── Interfaces/                  ← IRepository<T>, IStoreRepository, IProductRepository...
│   └── Enums/                       ← OrderStatus
│
├── AdminPortal.Application/         ← Application Business Rules
│   ├── Common/                      ← Result<T>, PagedResult<T>
│   ├── DTOs/                        ← StoreDto, ProductDto, OrderDto, AnalyticsDto...
│   ├── Interfaces/                  ← IStoreService, IProductService, IOrderService...
│   └── Services/                    ← StoreService, ProductService, OrderService...
│
├── AdminPortal.Infrastructure/      ← External Concerns (Data)
│   ├── MockData/                    ← MockDataStore (in-memory data)
│   ├── Repositories/                ← MockStoreRepository, MockProductRepository...
│   └── DependencyInjection.cs       ← AddInfrastructure(), AddApplication()
│
└── AdminPortal.Web/                 ← Presentation Layer
    ├── Controllers/                 ← DashboardController, ProductsController...
    ├── ViewModels/                  ← DashboardViewModel, ProductListViewModel...
    ├── Views/                       ← Razor .cshtml pages
    │   ├── Dashboard/Index.cshtml
    │   ├── Products/Index.cshtml
    │   ├── Settings/Index.cshtml
    │   ├── Payouts/Index.cshtml
    │   ├── Discounts/Index.cshtml
    │   └── Shared/_Layout.cshtml
    ├── wwwroot/
    │   ├── css/admin.css
    │   └── js/admin.js
    └── Program.cs
```

---

## Pages Implemented

| Page        | URL              | Features                                                 |
|-------------|------------------|----------------------------------------------------------|
| Dashboard   | `/`              | KPI cards, revenue chart, top products, recent orders    |
| Products    | `/Products`      | Grid view, search, filter, pagination, status toggle     |
| Settings    | `/Settings`      | Store details form, staff management, toggle store open  |
| Payouts     | `/Payouts`       | Balance, payout history table, request payout modal      |
| Discounts   | `/Discounts`     | Discount codes table, create modal, toggle/delete        |

---

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run

```bash
cd AdminPortal.Web
dotnet run
```

Then visit: `https://localhost:5001`

---

## SOLID Principles Applied

| Principle | Where |
|-----------|-------|
| **S**ingle Responsibility | Each service handles exactly one domain area |
| **O**pen/Closed | Switch from Mock → API: create new class implementing interface, register in DI — nothing else changes |
| **L**iskov Substitution | `MockProductRepository` is a drop-in for `IProductRepository` |
| **I**nterface Segregation | `IStoreRepository` extends `IRepository<Store>` only with store-specific methods |
| **D**ependency Inversion | Controllers depend on `IProductService` not `ProductService`; services depend on `IRepository` not concrete repos |

---

## Replacing Mock Data with a Real API

1. Create a new class, e.g. `ApiProductRepository : IProductRepository`
2. Inject `IHttpClientFactory` and call your backend endpoints
3. In `DependencyInjection.cs`, replace:
   ```csharp
   services.AddSingleton<IProductRepository, MockProductRepository>();
   // with:
   services.AddScoped<IProductRepository, ApiProductRepository>();
   ```
4. **No other files change.** Services, controllers, and views are completely unaffected.

---

## Tech Stack

- **ASP.NET Core 8 MVC** — controllers, Razor views, tag helpers
- **Bootstrap 5.3** — responsive grid & components  
- **Chart.js 4** — revenue chart on dashboard
- **Bootstrap Icons** — icon set throughout
- **DM Sans / DM Mono** — clean, professional typography
- **No ORM / No DB** — pure in-memory mock, ready for your API layer
