# WalletSystem — .NET Core 8 MVC — Complete Reference

## Quick Start

```bash
cd WalletSystem
dotnet run
# → http://localhost:5000
# SQLite DB auto-created + seeded on first run
```

---

## Project Structure

```
WalletSystem/
├── Models/
│   ├── User.cs                    Domain entity + UserStatus enum
│   └── Wallet.cs                  Wallet, Transaction entities + all enums
│
├── Data/
│   └── AppDbContext.cs            EF Core 8 context, relationships, seed data
│
├── ViewModels/
│   ├── ViewModels.cs              All VMs: List, Filter, Create, Edit, Dashboard
│   └── PagerVM.cs                 Reusable pager model
│
├── Services/
│   └── WalletService.cs           IWalletService + implementation
│
├── Controllers/
│   ├── HomeController.cs          Dashboard
│   ├── UsersController.cs         Full CRUD
│   ├── WalletsController.cs       Deposit/Withdraw/Transfer/Freeze/Unfreeze
│   └── TransactionsController.cs  Browse + Details
│
├── Views/
│   ├── Home/Index.cshtml          Dashboard — stats + recent txns
│   ├── Users/
│   │   ├── Index.cshtml           List + search/filter/sort/page
│   │   ├── Create.cshtml          Create form with wallet setup
│   │   ├── Edit.cshtml            Edit form
│   │   └── Details.cshtml         Profile + recent transactions
│   ├── Wallets/
│   │   ├── Index.cshtml           List + balance filter
│   │   ├── Details.cshtml         Full tx history with all filters
│   │   ├── Deposit.cshtml         Live balance preview
│   │   ├── Withdraw.cshtml        Quick % buttons + live preview
│   │   └── Transfer.cshtml        Recipient lookup + preview
│   ├── Transactions/
│   │   ├── Index.cshtml           Global ledger with all filters
│   │   └── Details.cshtml         Full detail + balance flow diagram
│   └── Shared/
│       ├── _Layout.cshtml         Sidebar shell
│       └── _Pager.cshtml          Reusable pager partial
│
├── wwwroot/
│   ├── css/site.css               Complete dark design system
│   └── js/site.js                 Alert dismiss, confirm dialogs
│
├── Program.cs                     DI, SQLite, auto-migrate
├── appsettings.json
└── WalletSystem.csproj            net8.0 + EF Sqlite
```

---

## Domain Models

### User
| Field        | Type         | Notes                          |
|-------------|-------------|-------------------------------|
| Id           | int          | PK                             |
| FullName     | string(100)  | Required                       |
| Email        | string(200)  | Unique index                   |
| PhoneNumber  | string(20)   | Required                       |
| Username     | string(50)   | Unique index                   |
| Status       | UserStatus   | Active / Inactive / Suspended  |
| CreatedAt    | DateTime     | UTC                            |
| UpdatedAt    | DateTime?    | Nullable                       |
| Wallet       | Wallet?      | 1:1 nav                        |

### Wallet
| Field      | Type          | Notes                         |
|-----------|--------------|------------------------------|
| Id         | int           | PK                            |
| UserId     | int           | FK → User, Unique             |
| Balance    | decimal(18,2) | Non-negative                  |
| Currency   | string(3)     | USD/EUR/GBP                   |
| Status     | WalletStatus  | Active / Frozen / Closed      |
| Transactions | ICollection | 1:M nav                      |

### Transaction
| Field          | Type               | Notes                                |
|---------------|-------------------|-------------------------------------|
| Id             | int                | PK                                   |
| WalletId       | int                | FK → Wallet                          |
| RelatedWalletId | int?              | FK → Wallet (for transfers)          |
| Type           | TransactionType    | Deposit/Withdrawal/Transfer/Refund/Fee/Bonus |
| Amount         | decimal(18,2)      | > 0                                  |
| BalanceBefore  | decimal(18,2)      | Audit: balance before this txn       |
| BalanceAfter   | decimal(18,2)      | Audit: balance after this txn        |
| Description    | string?(500)       | Optional                             |
| ReferenceNumber | string?(100)      | Unique, auto-generated               |
| Status         | TransactionStatus  | Pending/Completed/Failed/Reversed    |
| CreatedAt      | DateTime           | UTC                                  |

---

## Wallet Operations

### Deposit
- Validates: amount > 0, wallet Active
- Records BalanceBefore / BalanceAfter
- Generates ref: `TXN-YYYYMMDD-XXXXXXXX`

### Withdrawal
- Validates: amount > 0, wallet Active, Balance ≥ amount
- Quick % shortcuts (25/50/75/100) in UI

### Transfer
- Validates: both wallets Active, no self-transfer
- Looks up recipient by **username or email**
- Creates 2 transactions (debit + credit) with shared reference

### Freeze / Unfreeze
- Admin action, blocks all transactions on frozen wallets

---

## Paging / Search / Filter

### Users
| Filter    | Works on              |
|----------|-----------------------|
| Search    | Name, email, username, phone |
| Status    | Active / Inactive / Suspended |
| SortBy    | FullName, Email, Balance, CreatedAt |
| PageSize  | 5 / 10 / 25 / 50 |

### Wallets
| Filter     | Works on              |
|-----------|-----------------------|
| Search     | Owner name / email    |
| Status     | Active / Frozen / Closed |
| MinBalance | ≥ value               |
| MaxBalance | ≤ value               |

### Transactions (global + per-wallet)
| Filter    | Works on              |
|----------|-----------------------|
| Search    | Ref#, description, user |
| Type      | All 6 types           |
| Status    | All 4 statuses        |
| DateFrom  | ≥ date                |
| DateTo    | ≤ date                |
| MinAmount | ≥ value               |
| MaxAmount | ≤ value               |

---

## Best Practices Applied

| Practice | Implementation |
|----------|---------------|
| **Service Layer** | `IWalletService` separates business logic |
| **Repository via EF** | Includes, projections, no raw SQL |
| **Atomic operations** | Balance + transaction in one `SaveChangesAsync` |
| **Audit trail** | BalanceBefore + BalanceAfter on every transaction |
| **Unique constraints** | Email, Username, ReferenceNumber |
| **Decimal precision** | (18,2) on all money fields |
| **Cascade protection** | Can't delete user with balance > 0 |
| **CSRF protection** | `[ValidateAntiForgeryToken]` on all POST |
| **Model validation** | DataAnnotations + ModelState checks |
| **DI / IoC** | Services registered via `AddScoped` |
| **Enum-as-string** | EF converts enums to readable strings |
| **Seeded data** | 5 users, 5 wallets, 10 transactions |
| **Auto-migrate** | `db.Database.EnsureCreated()` on startup |
| **Projection** | `Select()` to VMs, never expose EF entities in views |

---

## Adding Auth (Next Steps)

```csharp
// Program.cs
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opts => { opts.LoginPath = "/Auth/Login"; });

// Controllers
[Authorize]
public class WalletsController : Controller { ... }

[Authorize(Roles = "Admin")]
public class UsersController : Controller { ... }
```

## Adding Migrations

```bash
dotnet tool install -g dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
```
