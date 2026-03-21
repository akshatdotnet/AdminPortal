namespace AdminPortal.Api.MockData;

// ── Enums ────────────────────────────────────────────────────────────────────

public enum OrderStatus { Pending, Confirmed, Shipped, Delivered, Cancelled }
public enum PayoutStatus { Pending, Processed, Failed }
public enum StaffRole { Admin, Manager, Support }

// ── Entities ─────────────────────────────────────────────────────────────────

public class Store
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Address { get; set; } = "";
    public bool IsOpen { get; set; }
    public string LogoUrl { get; set; } = "";
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Category { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime OrderDate { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class Discount
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Percentage { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public int UsageCount { get; set; }
    public int UsageLimit { get; set; }
}

public class Payout
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public PayoutStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string BankAccount { get; set; } = "";
    public string Reference { get; set; } = "";
}

public class StaffAccount
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public StaffRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class Audience
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime LastOrderDate { get; set; }
    public string Tag { get; set; } = "";
}

public class Credit
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Source { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime EarnedAt { get; set; }
    public bool IsUsed { get; set; }
}

// ── Mock Data Store ───────────────────────────────────────────────────────────

public class MockDataStore
{
    public Store Store { get; } = new Store
    {
        Id = 1,
        Name = "Vaishno Mart",
        Description = "Your one-stop shop for quality products",
        Category = "General Store",
        PhoneNumber = "+91 98765 43210",
        Address = "12, MG Road, Pune, Maharashtra 411001",
        IsOpen = true,
        LogoUrl = "/images/logo.png"
    };

    public List<Product> Products { get; } = new()
    {
        new() { Id=1, Name="Basmati Rice 5kg",    Price=449,  Stock=120, Category="Grocery",     IsActive=true,  CreatedAt=DateTime.Now.AddDays(-60), Description="Premium aged basmati rice",  ImageUrl="https://placehold.co/300x300?text=Rice"    },
        new() { Id=2, Name="Toor Dal 1kg",         Price=149,  Stock=200, Category="Grocery",     IsActive=true,  CreatedAt=DateTime.Now.AddDays(-55), Description="Fresh split pigeon peas",   ImageUrl="https://placehold.co/300x300?text=Dal"     },
        new() { Id=3, Name="Sunflower Oil 1L",     Price=189,  Stock=80,  Category="Grocery",     IsActive=true,  CreatedAt=DateTime.Now.AddDays(-50), Description="Refined sunflower oil",     ImageUrl="https://placehold.co/300x300?text=Oil"     },
        new() { Id=4, Name="Amul Butter 500g",     Price=299,  Stock=60,  Category="Dairy",       IsActive=true,  CreatedAt=DateTime.Now.AddDays(-45), Description="Pasteurised table butter",  ImageUrl="https://placehold.co/300x300?text=Butter"  },
        new() { Id=5, Name="Full Cream Milk 1L",   Price=68,   Stock=150, Category="Dairy",       IsActive=true,  CreatedAt=DateTime.Now.AddDays(-40), Description="Fresh full cream milk",     ImageUrl="https://placehold.co/300x300?text=Milk"    },
        new() { Id=6, Name="Surf Excel 2kg",       Price=399,  Stock=45,  Category="Household",   IsActive=true,  CreatedAt=DateTime.Now.AddDays(-35), Description="Detergent powder",          ImageUrl="https://placehold.co/300x300?text=Deterg"  },
        new() { Id=7, Name="Colgate 200g",         Price=89,   Stock=90,  Category="Personal Care",IsActive=true, CreatedAt=DateTime.Now.AddDays(-30), Description="Strong teeth toothpaste",   ImageUrl="https://placehold.co/300x300?text=Paste"   },
        new() { Id=8, Name="Lay's Classic 100g",   Price=30,   Stock=300, Category="Snacks",      IsActive=true,  CreatedAt=DateTime.Now.AddDays(-25), Description="Salted potato chips",       ImageUrl="https://placehold.co/300x300?text=Chips"   },
        new() { Id=9, Name="Maggi 2-Min Noodles",  Price=14,   Stock=500, Category="Snacks",      IsActive=true,  CreatedAt=DateTime.Now.AddDays(-20), Description="Instant noodles 70g",       ImageUrl="https://placehold.co/300x300?text=Maggi"   },
        new() { Id=10,Name="Dettol Handwash 200ml",Price=99,   Stock=0,   Category="Personal Care",IsActive=false,CreatedAt=DateTime.Now.AddDays(-15), Description="Antibacterial handwash",    ImageUrl="https://placehold.co/300x300?text=Dettol"  },
    };

    public List<Order> Orders { get; } = new()
    {
        new() { Id=1,  OrderNumber="ORD-1001", CustomerName="Rahul Sharma",  CustomerPhone="+91 9876543210", TotalAmount=638,  Status=OrderStatus.Delivered,  OrderDate=DateTime.Now.AddDays(-1),  Items=new(){ new(){ ProductId=1, ProductName="Basmati Rice 5kg",  Quantity=1, UnitPrice=449 }, new(){ ProductId=2, ProductName="Toor Dal 1kg", Quantity=1, UnitPrice=149 } } },
        new() { Id=2,  OrderNumber="ORD-1002", CustomerName="Priya Mehta",   CustomerPhone="+91 9123456789", TotalAmount=299,  Status=OrderStatus.Shipped,    OrderDate=DateTime.Now.AddDays(-1),  Items=new(){ new(){ ProductId=4, ProductName="Amul Butter 500g", Quantity=1, UnitPrice=299 } } },
        new() { Id=3,  OrderNumber="ORD-1003", CustomerName="Amit Patel",    CustomerPhone="+91 9988776655", TotalAmount=896,  Status=OrderStatus.Confirmed,  OrderDate=DateTime.Now.AddHours(-5), Items=new(){ new(){ ProductId=3, ProductName="Sunflower Oil 1L",  Quantity=2, UnitPrice=189 }, new(){ ProductId=6, ProductName="Surf Excel 2kg", Quantity=1, UnitPrice=399 } } },
        new() { Id=4,  OrderNumber="ORD-1004", CustomerName="Sunita Rao",    CustomerPhone="+91 9871234560", TotalAmount=204,  Status=OrderStatus.Pending,    OrderDate=DateTime.Now.AddHours(-2), Items=new(){ new(){ ProductId=8, ProductName="Lay's Classic 100g", Quantity=3, UnitPrice=30  }, new(){ ProductId=9, ProductName="Maggi 2-Min Noodles", Quantity=9, UnitPrice=14 } } },
        new() { Id=5,  OrderNumber="ORD-1005", CustomerName="Vikram Singh",  CustomerPhone="+91 9001122334", TotalAmount=189,  Status=OrderStatus.Cancelled,  OrderDate=DateTime.Now.AddDays(-2),  Items=new(){ new(){ ProductId=3, ProductName="Sunflower Oil 1L",  Quantity=1, UnitPrice=189 } } },
        new() { Id=6,  OrderNumber="ORD-1006", CustomerName="Anjali Gupta",  CustomerPhone="+91 9334455667", TotalAmount=1046, Status=OrderStatus.Delivered,  OrderDate=DateTime.Now.AddDays(-3),  Items=new(){ new(){ ProductId=1, ProductName="Basmati Rice 5kg",  Quantity=1, UnitPrice=449 }, new(){ ProductId=4, ProductName="Amul Butter 500g", Quantity=1, UnitPrice=299 }, new(){ ProductId=5, ProductName="Full Cream Milk 1L", Quantity=4, UnitPrice=68 } } },
        new() { Id=7,  OrderNumber="ORD-1007", CustomerName="Deepak Joshi",  CustomerPhone="+91 9445566778", TotalAmount=399,  Status=OrderStatus.Delivered,  OrderDate=DateTime.Now.AddDays(-4),  Items=new(){ new(){ ProductId=6, ProductName="Surf Excel 2kg",    Quantity=1, UnitPrice=399 } } },
        new() { Id=8,  OrderNumber="ORD-1008", CustomerName="Meena Iyer",    CustomerPhone="+91 9556677889", TotalAmount=356,  Status=OrderStatus.Shipped,    OrderDate=DateTime.Now.AddDays(-4),  Items=new(){ new(){ ProductId=7, ProductName="Colgate 200g",      Quantity=2, UnitPrice=89  }, new(){ ProductId=8, ProductName="Lay's Classic 100g", Quantity=3, UnitPrice=30  }, new(){ ProductId=9, ProductName="Maggi 2-Min Noodles", Quantity=9, UnitPrice=14 } } },
    };

    public List<Discount> Discounts { get; } = new()
    {
        new() { Id=1, Code="WELCOME10", Description="New customer welcome discount", Percentage=10, ExpiryDate=DateTime.Now.AddDays(30),  IsActive=true,  UsageCount=42,  UsageLimit=100  },
        new() { Id=2, Code="SAVE20",    Description="Flat 20% on orders above ₹500", Percentage=20, ExpiryDate=DateTime.Now.AddDays(15),  IsActive=true,  UsageCount=118, UsageLimit=200  },
        new() { Id=3, Code="DAIRY15",   Description="15% off on all dairy products",  Percentage=15, ExpiryDate=DateTime.Now.AddDays(7),   IsActive=true,  UsageCount=33,  UsageLimit=50   },
        new() { Id=4, Code="BULK30",    Description="30% off on bulk purchases",       Percentage=30, ExpiryDate=DateTime.Now.AddDays(-5),  IsActive=false, UsageCount=200, UsageLimit=200  },
        new() { Id=5, Code="FESTIVE25", Description="Festival special offer",          Percentage=25, ExpiryDate=DateTime.Now.AddDays(60),  IsActive=false, UsageCount=0,   UsageLimit=500  },
    };

    public List<Payout> Payouts { get; } = new()
    {
        new() { Id=1, Amount=12500, Status=PayoutStatus.Processed, RequestedAt=DateTime.Now.AddDays(-30), ProcessedAt=DateTime.Now.AddDays(-28), BankAccount="XXXX1234", Reference="PAY-20240101" },
        new() { Id=2, Amount=8750,  Status=PayoutStatus.Processed, RequestedAt=DateTime.Now.AddDays(-20), ProcessedAt=DateTime.Now.AddDays(-18), BankAccount="XXXX1234", Reference="PAY-20240111" },
        new() { Id=3, Amount=15200, Status=PayoutStatus.Processed, RequestedAt=DateTime.Now.AddDays(-10), ProcessedAt=DateTime.Now.AddDays(-8),  BankAccount="XXXX1234", Reference="PAY-20240121" },
        new() { Id=4, Amount=6300,  Status=PayoutStatus.Pending,   RequestedAt=DateTime.Now.AddDays(-2),  ProcessedAt=null,                      BankAccount="XXXX1234", Reference="PAY-20240129" },
    };

    public List<StaffAccount> Staff { get; } = new()
    {
        new() { Id=1, Name="Ravi Kumar",     Email="ravi@vaishnomart.com",   Role=StaffRole.Admin,   IsActive=true,  JoinedAt=DateTime.Now.AddYears(-2)  },
        new() { Id=2, Name="Sita Devi",      Email="sita@vaishnomart.com",   Role=StaffRole.Manager, IsActive=true,  JoinedAt=DateTime.Now.AddYears(-1)  },
        new() { Id=3, Name="Mohan Lal",      Email="mohan@vaishnomart.com",  Role=StaffRole.Support, IsActive=true,  JoinedAt=DateTime.Now.AddMonths(-6) },
        new() { Id=4, Name="Geeta Sharma",   Email="geeta@vaishnomart.com",  Role=StaffRole.Support, IsActive=false, JoinedAt=DateTime.Now.AddMonths(-3) },
    };

    public List<Audience> Audience { get; } = new()
    {
        new() { Id=1, Name="Rahul Sharma",  Phone="+91 9876543210", Email="rahul@email.com",  TotalOrders=12, TotalSpent=6842,  LastOrderDate=DateTime.Now.AddDays(-1),  Tag="Loyal"    },
        new() { Id=2, Name="Priya Mehta",   Phone="+91 9123456789", Email="priya@email.com",  TotalOrders=4,  TotalSpent=1299,  LastOrderDate=DateTime.Now.AddDays(-1),  Tag="Regular"  },
        new() { Id=3, Name="Amit Patel",    Phone="+91 9988776655", Email="amit@email.com",   TotalOrders=8,  TotalSpent=4200,  LastOrderDate=DateTime.Now.AddHours(-5), Tag="Regular"  },
        new() { Id=4, Name="Sunita Rao",    Phone="+91 9871234560", Email="sunita@email.com", TotalOrders=2,  TotalSpent=404,   LastOrderDate=DateTime.Now.AddHours(-2), Tag="New"      },
        new() { Id=5, Name="Vikram Singh",  Phone="+91 9001122334", Email="vikram@email.com", TotalOrders=1,  TotalSpent=0,     LastOrderDate=DateTime.Now.AddDays(-2),  Tag="Churned"  },
        new() { Id=6, Name="Anjali Gupta",  Phone="+91 9334455667", Email="anjali@email.com", TotalOrders=20, TotalSpent=18400, LastOrderDate=DateTime.Now.AddDays(-3),  Tag="VIP"      },
    };

    public List<Credit> Credits { get; } = new()
    {
        new() { Id=1, Amount=250, Source="Cashback",   Description="Cashback on order ORD-1001", EarnedAt=DateTime.Now.AddDays(-30), IsUsed=true  },
        new() { Id=2, Amount=100, Source="Referral",   Description="Referral bonus - Priya Mehta",EarnedAt=DateTime.Now.AddDays(-20), IsUsed=true  },
        new() { Id=3, Amount=500, Source="Promotion",  Description="Festival season credit",      EarnedAt=DateTime.Now.AddDays(-10), IsUsed=false },
        new() { Id=4, Amount=150, Source="Cashback",   Description="Cashback on order ORD-1006", EarnedAt=DateTime.Now.AddDays(-3),  IsUsed=false },
    };

    // ── Computed analytics ───────────────────────────────────────────────────

    public decimal TotalRevenue => Orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount);
    public int TotalOrders => Orders.Count;
    public int TotalProducts => Products.Count;
    public decimal AvailableBalance => 4800m; // simulated wallet balance
}
