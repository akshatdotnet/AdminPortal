using AdminPortal.Domain.Entities;

namespace AdminPortal.Infrastructure.MockData;

public static class MockDataStore
{
    public static List<Store> Stores { get; } = new()
    {
        new Store
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000001"),
            StoreLink = "mydukaan.io/www232",
            StoreName = "Shopzo",
            MobileNumber = "+91 9876543210",
            EmailAddress = "pradeep.g25@gmail.com",
            Country = "India",
            Currency = "INR",
            StoreAddress = "Hiranandani Gardens, Central Ave, Hiranandani Gardens, Panchkutir Ganesh Nagar, Mumbai - 400076",
            IsOpen = true,
            CreatedAt = DateTime.UtcNow.AddYears(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-3)
        }
    };

    public static List<Product> Products { get; } = new()
    {
        new Product { Id = Guid.NewGuid(), Name = "Kaju Katli Premium Box", Description = "Premium quality kaju katli, 500g box with silver varq.", Price = 850, DiscountedPrice = 720, Stock = 45, Category = "Sweets", ImageUrl = "", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-120) },
        new Product { Id = Guid.NewGuid(), Name = "Dry Fruit Gift Hamper", Description = "Assorted dry fruits in a decorative wooden box.", Price = 2200, DiscountedPrice = 1899, Stock = 18, Category = "Gift Sets", ImageUrl = "", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-90) },
        new Product { Id = Guid.NewGuid(), Name = "Badam Milk Powder", Description = "Pure almond milk powder, 250g premium pack.", Price = 450, Stock = 72, Category = "Beverages", ImageUrl = "", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-60) },
        new Product { Id = Guid.NewGuid(), Name = "Mixed Namkeen 1kg", Description = "Traditional Bombay mix namkeen with premium spices.", Price = 320, DiscountedPrice = 280, Stock = 120, Category = "Snacks", ImageUrl = "", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-45) },
        new Product { Id = Guid.NewGuid(), Name = "Rose Ladoo Box", Description = "Handmade besan ladoo with rose flavour, 12 pieces.", Price = 560, Stock = 33, Category = "Sweets", ImageUrl = "", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-30) },
        new Product { Id = Guid.NewGuid(), Name = "Saffron Kheer Mix", Description = "Ready-to-cook saffron rice kheer mix.", Price = 199, DiscountedPrice = 169, Stock = 95, Category = "Beverages", ImageUrl = "", IsActive = false, CreatedAt = DateTime.UtcNow.AddDays(-20) },
        new Product { Id = Guid.NewGuid(), Name = "Festive Combo Pack", Description = "Diwali special combo with 5 assorted sweets.", Price = 1200, DiscountedPrice = 999, Stock = 0, Category = "Gift Sets", ImageUrl = "", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-10) },
        new Product { Id = Guid.NewGuid(), Name = "Mathri Classic Pack", Description = "Crispy traditional mathri, 400g.", Price = 180, Stock = 200, Category = "Snacks", ImageUrl = "", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-5) }
    };

    public static List<Order> Orders { get; } = new()
    {
        CreateOrder("#ORD-2401", "Ananya Sharma",  "ananya@email.com",   OrderStatus.Delivered,  "UPI",  -5,  1720m),
        CreateOrder("#ORD-2402", "Rahul Mehta",    "rahul.m@email.com",  OrderStatus.Processing, "Card", -3,  1000m),
        CreateOrder("#ORD-2403", "Priya Nair",     "priya.n@email.com",  OrderStatus.Shipped,    "COD",  -2,  560m),
        CreateOrder("#ORD-2404", "Vikram Singh",   "vikram.s@email.com", OrderStatus.Pending,    "UPI",  -1,  850m),
        CreateOrder("#ORD-2405", "Sunita Patel",   "sunita.p@email.com", OrderStatus.Delivered,  "Card", -10, 2200m),
        CreateOrder("#ORD-2406", "Arun Kumar",     "arun.k@email.com",   OrderStatus.Cancelled,  "UPI",  -7,  320m),
        CreateOrder("#ORD-2407", "Deepa Iyer",     "deepa.i@email.com",  OrderStatus.Delivered,  "UPI",  -15, 1899m),
        CreateOrder("#ORD-2408", "Kartik Joshi",   "kartik.j@email.com", OrderStatus.Processing, "Card", -1,  720m),
        CreateOrder("#ORD-2409", "Meena Desai",    "meena.d@email.com",  OrderStatus.Delivered,  "UPI",  -18, 999m),
        CreateOrder("#ORD-2410", "Suresh Reddy",   "suresh.r@email.com", OrderStatus.Shipped,    "Card", -4,  450m),
        CreateOrder("#ORD-2411", "Lakshmi Bhat",   "lakshmi.b@email.com",OrderStatus.Pending,    "COD",  -1,  280m),
        CreateOrder("#ORD-2412", "Rajesh Kumar",   "rajesh.k@email.com", OrderStatus.Delivered,  "UPI",  -22, 1200m),
    };

    public static List<Customer> Customers { get; } = new()
    {
        new Customer { Id = Guid.NewGuid(), Name = "Ashish Gupta",  MobileNumber = "+91-9766422322", Email = "ashishrg12@gmail.com",    City = "Raigarh",     State = "Chhattisgarh", Type = CustomerType.New,       TotalOrders = 1, TotalSales = 175,  CreatedAt = DateTime.UtcNow.AddMonths(-2), LastOrderAt = DateTime.UtcNow.AddDays(-5)  },
        new Customer { Id = Guid.NewGuid(), Name = "Akshat Kumar",  MobileNumber = "+91-8097944981", Email = "akshatdotnet@gmail.com",  City = "Navi Mumbai", State = "Maharashtra",  Type = CustomerType.Returning, TotalOrders = 2, TotalSales = 0,    CreatedAt = DateTime.UtcNow.AddMonths(-6), LastOrderAt = DateTime.UtcNow.AddDays(-15) },
        new Customer { Id = Guid.NewGuid(), Name = "Priya Nair",    MobileNumber = "+91-9823456712", Email = "priya.n@email.com",       City = "Pune",        State = "Maharashtra",  Type = CustomerType.Returning, TotalOrders = 4, TotalSales = 3200, CreatedAt = DateTime.UtcNow.AddYears(-1),  LastOrderAt = DateTime.UtcNow.AddDays(-2)  },
        new Customer { Id = Guid.NewGuid(), Name = "Rahul Mehta",   MobileNumber = "+91-9012345678", Email = "rahul.m@email.com",       City = "Mumbai",      State = "Maharashtra",  Type = CustomerType.Returning, TotalOrders = 3, TotalSales = 2850, CreatedAt = DateTime.UtcNow.AddMonths(-9), LastOrderAt = DateTime.UtcNow.AddDays(-3)  },
        new Customer { Id = Guid.NewGuid(), Name = "Ananya Sharma", MobileNumber = "+91-8812345670", Email = "ananya@email.com",        City = "Delhi",       State = "Delhi",        Type = CustomerType.New,       TotalOrders = 1, TotalSales = 1720, CreatedAt = DateTime.UtcNow.AddDays(-10),  LastOrderAt = DateTime.UtcNow.AddDays(-5)  },
        new Customer { Id = Guid.NewGuid(), Name = "Vikram Singh",  MobileNumber = "+91-7654321098", Email = "vikram.s@email.com",      City = "Bangalore",   State = "Karnataka",    Type = CustomerType.New,       TotalOrders = 1, TotalSales = 850,  CreatedAt = DateTime.UtcNow.AddDays(-1),   LastOrderAt = null                         },
        new Customer { Id = Guid.NewGuid(), Name = "Sunita Patel",  MobileNumber = "+91-9345678901", Email = "sunita.p@email.com",      City = "Ahmedabad",   State = "Gujarat",      Type = CustomerType.Returning, TotalOrders = 5, TotalSales = 5800, CreatedAt = DateTime.UtcNow.AddYears(-2),  LastOrderAt = DateTime.UtcNow.AddDays(-10) },
        new Customer { Id = Guid.NewGuid(), Name = "Deepa Iyer",    MobileNumber = "+91-9987654321", Email = "deepa.i@email.com",       City = "Chennai",     State = "Tamil Nadu",   Type = CustomerType.Imported,  TotalOrders = 2, TotalSales = 1899, CreatedAt = DateTime.UtcNow.AddMonths(-3), LastOrderAt = DateTime.UtcNow.AddDays(-15) },
    };

    public static List<StaffAccount> Staff { get; } = new()
    {
        new StaffAccount { Id = Guid.NewGuid(), Name = "Pradeep Gupta", Email = "pradeep.g25@gmail.com", Role = "Owner",   IsActive = true, JoinedAt = DateTime.UtcNow.AddYears(-2),  LastLoginAt = DateTime.UtcNow.AddHours(-2) },
        new StaffAccount { Id = Guid.NewGuid(), Name = "Meena Sharma",  Email = "meena.s@shopzo.com",    Role = "Manager", IsActive = true, JoinedAt = DateTime.UtcNow.AddMonths(-8), LastLoginAt = DateTime.UtcNow.AddDays(-1)  },
        new StaffAccount { Id = Guid.NewGuid(), Name = "Rajan Patel",   Email = "rajan.p@shopzo.com",    Role = "Staff",   IsActive = true, JoinedAt = DateTime.UtcNow.AddMonths(-3), LastLoginAt = DateTime.UtcNow.AddDays(-3)  },
        new StaffAccount { Id = Guid.NewGuid(), Name = "Lata Verma",    Email = "lata.v@shopzo.com",     Role = "Staff",   IsActive = false,JoinedAt = DateTime.UtcNow.AddMonths(-1), LastLoginAt = null                         },
    };

    public static List<Discount> Discounts { get; } = new()
    {
        new Discount { Id = Guid.NewGuid(), Code = "DIWALI25", Description = "Diwali special 25% off",   Type = DiscountType.Percentage,  Value = 25,  UsageLimit = 500,  UsedCount = 312, ExpiresAt = DateTime.UtcNow.AddDays(15),  IsActive = true,  CreatedAt = DateTime.UtcNow.AddDays(-30) },
        new Discount { Id = Guid.NewGuid(), Code = "FIRST100", Description = "First order ₹100 off",     Type = DiscountType.FixedAmount, Value = 100, UsageLimit = 1000, UsedCount = 847, IsActive = true,  CreatedAt = DateTime.UtcNow.AddMonths(-3) },
        new Discount { Id = Guid.NewGuid(), Code = "SWEET10",  Description = "10% off on all sweets",    Type = DiscountType.Percentage,  Value = 10,  UsedCount = 56,                     IsActive = true,  CreatedAt = DateTime.UtcNow.AddDays(-7)  },
        new Discount { Id = Guid.NewGuid(), Code = "SUMMER50", Description = "Summer sale ₹50 off",      Type = DiscountType.FixedAmount, Value = 50,  UsageLimit = 200,  UsedCount = 200, ExpiresAt = DateTime.UtcNow.AddDays(-10), IsActive = false, CreatedAt = DateTime.UtcNow.AddMonths(-2) },
    };

    public static List<Payout> Payouts { get; } = new()
    {
        new Payout { Id = Guid.NewGuid(), Amount = 45000,  BankAccount = "HDFC ****1234", Status = PayoutStatus.Completed,  RequestedAt = DateTime.UtcNow.AddDays(-30), ProcessedAt = DateTime.UtcNow.AddDays(-28), TransactionRef = "TXN20240115093045" },
        new Payout { Id = Guid.NewGuid(), Amount = 32500,  BankAccount = "HDFC ****1234", Status = PayoutStatus.Completed,  RequestedAt = DateTime.UtcNow.AddDays(-15), ProcessedAt = DateTime.UtcNow.AddDays(-13), TransactionRef = "TXN20240201114523" },
        new Payout { Id = Guid.NewGuid(), Amount = 18750,  BankAccount = "HDFC ****1234", Status = PayoutStatus.Processing, RequestedAt = DateTime.UtcNow.AddDays(-2),  TransactionRef = "TXN20240214082311" },
        new Payout { Id = Guid.NewGuid(), Amount = 9200,   BankAccount = "HDFC ****1234", Status = PayoutStatus.Pending,    RequestedAt = DateTime.UtcNow.AddHours(-6), TransactionRef = "TXN20240217153901" },
    };

    public static List<CreditTransaction> CreditTransactions { get; } = new()
    {
        new CreditTransaction { Id = Guid.NewGuid(), ReferenceId = "#3525883", Details = "Dukaan service charges",       OrderId = "#22670283", Credits = 2.4m,  Type = TransactionType.Debit,  Balance = 88.87m, CreatedAt = DateTime.Parse("2026-01-27 23:15") },
        new CreditTransaction { Id = Guid.NewGuid(), ReferenceId = "#3489187", Details = "Dukaan service charges",       OrderId = "#22491842", Credits = 8.73m, Type = TransactionType.Debit,  Balance = 91.27m, CreatedAt = DateTime.Parse("2026-01-09 12:33") },
        new CreditTransaction { Id = Guid.NewGuid(), ReferenceId = "#3488798", Details = "Dukaan service charge refund", OrderId = "#22490345", Credits = 2.99m, Type = TransactionType.Credit, Balance = 100m,   CreatedAt = DateTime.Parse("2026-01-09 03:50") },
        new CreditTransaction { Id = Guid.NewGuid(), ReferenceId = "#3488784", Details = "Dukaan service charges",       OrderId = "#22490345", Credits = 2.99m, Type = TransactionType.Debit,  Balance = 97.01m, CreatedAt = DateTime.Parse("2026-01-09 03:13") },
        new CreditTransaction { Id = Guid.NewGuid(), ReferenceId = "#3488760", Details = "Credits from Dukaan",          OrderId = null,        Credits = 100m,  Type = TransactionType.Credit, Balance = 100m,   CreatedAt = DateTime.Parse("2026-01-09 02:30") },
        new CreditTransaction { Id = Guid.NewGuid(), ReferenceId = "#3412345", Details = "Dukaan service charges",       OrderId = "#22380001", Credits = 5.50m, Type = TransactionType.Debit,  Balance = 0m,     CreatedAt = DateTime.UtcNow.AddDays(-35) },
    };

    private static Order CreateOrder(string orderNum, string name, string email, OrderStatus status, string payment, int daysAgo, decimal total)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNum,
            CustomerName = name,
            CustomerEmail = email,
            TotalAmount = total,
            Status = status,
            PaymentMethod = payment,
            OrderDate = DateTime.UtcNow.AddDays(daysAgo),
            Items = new List<OrderItem>
            {
                new() { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Kaju Katli Premium Box", Quantity = 1, UnitPrice = total }
            }
        };
    }
}
