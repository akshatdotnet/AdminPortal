# Zovo — E-commerce Admin Panel
**ASP.NET Core 8 MVC · Clean Architecture · EF Core 8 · In-Memory DB (zero setup)**

---

## ⚡ Run in 3 Commands

```bash
git clone <repo>  # or unzip the downloaded folder
cd Zovo/src/Zovo.Web
dotnet run
```

Then open **https://localhost:5001** in your browser.

> **No SQL Server needed.** The app uses an in-memory database seeded with sample data.

---

## 🖥️ Prerequisites

| Tool | Version | Download |
|------|---------|----------|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download |
| Any browser | Modern | Chrome, Edge, Firefox |

Check your version: `dotnet --version`  (needs 8.x.x)

---

## 📁 Project Structure

```
Zovo/
├── Zovo.sln                          ← Open this in Visual Studio
└── src/
    ├── Zovo.Core/                    ← Domain layer (no dependencies)
    │   ├── Entities/                 ← Product, Customer, Order, Address, StoreSettings
    │   ├── Enums/                    ← OrderStatus, PaymentStatus, CustomerStatus
    │   ├── Interfaces/               ← IRepository<T>, IUnitOfWork, specialised repos
    │   └── ValueObjects/             ← PagedResult<T>, Result, Result<T>
    │
    ├── Zovo.Infrastructure/          ← Data access layer
    │   ├── Data/ZovoDbContext.cs     ← EF Core context with full Fluent API
    │   ├── Data/Seeding/ZovoSeed.cs  ← Sample data (8 products, 5 customers, 5 orders)
    │   ├── Repositories/             ← Generic + specialised repository implementations
    │   ├── UnitOfWork.cs             ← Transaction wrapper for all repos
    │   └── InfrastructureServiceExtensions.cs ← DI extension: AddInfrastructure()
    │
    ├── Zovo.Application/             ← Business logic layer
    │   ├── Products/                 ← DTOs, CreateProductCommand, IProductService, ProductService
    │   ├── Orders/                   ← DTOs, CreateOrderCommand, IOrderService, OrderService
    │   ├── Customers/                ← DTOs, CreateCustomerCommand, ICustomerService, CustomerService
    │   ├── Dashboard/                ← DashboardSummary, IDashboardService, DashboardService
    │   └── ApplicationServiceExtensions.cs ← DI extension: AddApplication()
    │
    └── Zovo.Web/                     ← ASP.NET Core 8 MVC
        ├── Controllers/              ← Thin controllers (7 controllers)
        ├── Views/                    ← Razor views (15 views + layout)
        │   ├── Dashboard/Index       ← KPI stats + recent orders + low stock
        │   ├── Products/             ← Index (table+filters), Form (create/edit), Detail
        │   ├── Orders/               ← Index (table+filters), Detail (items+payment)
        │   ├── Customers/            ← Index, Form (create/edit), Detail
        │   ├── Analytics/Index       ← Chart.js bar + doughnut + category table
        │   └── Shared/_Layout        ← Dark sidebar + topbar + delete modal
        ├── wwwroot/css/zovo.css      ← Full design system (Sora + IBM Plex Sans)
        ├── wwwroot/js/zovo.js        ← AJAX handlers, dropdown, toast, delete modal
        └── Program.cs                ← DI composition root

```

---

## 🔧 Configuration

### Switch to SQL Server (Production)

1. **Program.cs** — change `useInMemory: true` → `useInMemory: false`
2. **appsettings.json** — update the connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER;Database=ZovoDB;Trusted_Connection=True;"
   }
   ```
3. **Run migrations:**
   ```bash
   cd src/Zovo.Web
   dotnet ef migrations add InitialCreate --project ../Zovo.Infrastructure
   dotnet ef database update
   ```

### Open in Visual Studio
Double-click `Zovo.sln` → Set `Zovo.Web` as startup project → Press F5.

### Open in VS Code
```bash
cd Zovo
code .
# Install C# Dev Kit extension when prompted
# Press F5 to run
```

---

## 🌐 Available Pages

| URL | Page |
|-----|------|
| `/` | Dashboard — KPIs, recent orders, low stock |
| `/Products` | Product list with filters, toggle, CRUD |
| `/Products/Create` | Add new product |
| `/Products/Edit/1` | Edit product |
| `/Products/Detail/1` | Product detail + margin calc |
| `/Orders` | Orders list with status/payment filters |
| `/Orders/Detail/1` | Order detail with line items |
| `/Customers` | Customer list |
| `/Customers/Create` | Add customer |
| `/Customers/Detail/1` | Customer profile + addresses |
| `/Analytics` | Revenue chart + category breakdown |
| `/Store` | Store settings (stub — ready to extend) |
| `/Account/ChangePassword` | Security (stub — add Identity) |

---

## ✨ Features

- **Dashboard** — 4 KPI stat cards, recent orders table, low stock alert panel
- **Products** — Full CRUD · filter by category/status · sort 6 ways · paginate · live toggle (AJAX) · animated delete · featured flag · 3-tier pricing (price/compare-at/cost) · margin calculator
- **Orders** — Status progression (Pending→Confirmed→Processing→Shipped→Delivered) · AJAX update · auto-timestamps · stock restore on cancel · payment status tracking
- **Customers** — CRUD · email uniqueness validation · order count + spend totals · address book · status toggle
- **Analytics** — Chart.js bar chart (monthly revenue) + doughnut chart (inventory by category)
- **Design** — Sora + IBM Plex Sans + JetBrains Mono · Deep slate sidebar (#0d1117) · Teal accent (#0ea5a0) · Fully responsive

---

## 🏗️ Architecture Decisions

| Decision | Why |
|----------|-----|
| **4-layer Clean Architecture** | Domain, Infrastructure, Application, Web each have clear boundaries and single responsibility |
| **Unit of Work pattern** | All repos share one `DbContext`, transactions are atomic |
| **Result<T> instead of exceptions** | Expected failures (not found, duplicate email) return Result.Fail() — cleaner controller code |
| **ViewModels / DTOs** | EF entities never reach views — prevents over-posting and couples persistence to UI |
| **Interface-first services** | All services registered as interfaces — easy to mock for testing, easy to swap implementations |
| **In-memory DB default** | Zero setup friction for development and demos |

---

## 🚀 Production Checklist

- [ ] Set `useInMemory: false` + configure SQL Server connection string
- [ ] Run EF Core migrations
- [ ] Add `Microsoft.AspNetCore.Identity` for authentication
- [ ] Add image upload (IFormFile + Azure Blob / AWS S3)
- [ ] Add Serilog for structured logging
- [ ] Add `IMemoryCache` for category list caching in ProductService
- [ ] Set up HTTPS certificate
- [ ] Add Docker support (`Dockerfile` + `docker-compose.yml`)
- [ ] Configure CI/CD pipeline

