using Microsoft.EntityFrameworkCore;
using Zovo.Core.Entities;
using Zovo.Core.Enums;

namespace Zovo.Infrastructure.Data.Seeding;

public static class ZovoSeed
{
    private static readonly DateTime _now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static void Apply(ModelBuilder mb)
    {
        mb.Entity<StoreSettings>().HasData(new StoreSettings {
            Id = 1, StoreName = "Zovo Store", Currency = "INR", CurrencySymbol = "₹",
            TimeZone = "Asia/Kolkata", TaxRate = 18m, FreeShippingThreshold = 500m, DefaultShippingCost = 49m
        });

        mb.Entity<Customer>().HasData(
            new Customer { Id=1, FirstName="Priya",  LastName="Sharma",  Email="priya@example.com",  Phone="9876543210", Status=CustomerStatus.Active,   CreatedAt=_now, UpdatedAt=_now },
            new Customer { Id=2, FirstName="Rohan",  LastName="Mehta",   Email="rohan@example.com",  Phone="9876543211", Status=CustomerStatus.Active,   CreatedAt=_now, UpdatedAt=_now },
            new Customer { Id=3, FirstName="Aarav",  LastName="Patel",   Email="aarav@example.com",  Phone="9876543212", Status=CustomerStatus.Active,   CreatedAt=_now, UpdatedAt=_now },
            new Customer { Id=4, FirstName="Sanya",  LastName="Kapoor",  Email="sanya@example.com",  Phone="9876543213", Status=CustomerStatus.Active,   CreatedAt=_now, UpdatedAt=_now },
            new Customer { Id=5, FirstName="Vikram", LastName="Singh",   Email="vikram@example.com", Phone="9876543214", Status=CustomerStatus.Inactive, CreatedAt=_now, UpdatedAt=_now }
        );

        mb.Entity<Product>().HasData(
            new Product { Id=1, Name="Cotton T-Shirt",        SKU="CLO-001", Slug="cotton-t-shirt",         Category="Clothing",          Price=299m,  Stock=100, IsActive=true,  IsFeatured=true,  Weight=0.3m,  LowStockThreshold=20, CreatedAt=_now, UpdatedAt=_now },
            new Product { Id=2, Name="Premium Green Tea",     SKU="FNB-001", Slug="premium-green-tea",      Category="Food & Beverages",  Price=149m,  Stock=200, IsActive=true,  IsFeatured=true,  Weight=0.2m,  LowStockThreshold=30, CreatedAt=_now, UpdatedAt=_now },
            new Product { Id=3, Name="USB-C Hub 7-in-1",      SKU="ELC-001", Slug="usb-c-hub-7-in-1",       Category="Electronics",       Price=1499m, Stock=30,  IsActive=true,  IsFeatured=true,  Weight=0.15m, LowStockThreshold=10, CreatedAt=_now, UpdatedAt=_now },
            new Product { Id=4, Name="Wireless Earbuds",      SKU="ELC-002", Slug="wireless-earbuds",       Category="Electronics",       Price=999m,  Stock=50,  IsActive=true,  IsFeatured=false, Weight=0.1m,  LowStockThreshold=10, CreatedAt=_now, UpdatedAt=_now },
            new Product { Id=5, Name="Yoga Mat Pro",          SKU="SPT-001", Slug="yoga-mat-pro",           Category="Sports",            Price=799m,  Stock=8,   IsActive=true,  IsFeatured=false, Weight=1.2m,  LowStockThreshold=10, CreatedAt=_now, UpdatedAt=_now },
            new Product { Id=6, Name="Stainless Steel Bottle",SKU="HOM-001", Slug="stainless-steel-bottle", Category="Home & Kitchen",    Price=449m,  Stock=5,   IsActive=true,  IsFeatured=false, Weight=0.4m,  LowStockThreshold=10, CreatedAt=_now, UpdatedAt=_now },
            new Product { Id=7, Name="Running Shoes",         SKU="CLO-002", Slug="running-shoes",          Category="Clothing",          Price=2499m, Stock=25,  IsActive=true,  IsFeatured=true,  Weight=0.8m,  LowStockThreshold=10, CreatedAt=_now, UpdatedAt=_now },
            new Product { Id=8, Name="Bluetooth Speaker",     SKU="ELC-003", Slug="bluetooth-speaker",      Category="Electronics",       Price=1299m, Stock=0,   IsActive=false, IsFeatured=false, Weight=0.6m,  LowStockThreshold=10, CreatedAt=_now, UpdatedAt=_now }
        );

        mb.Entity<Order>().HasData(
            new Order { Id=1, OrderNumber="ZOV-000001", CustomerId=1, Status=OrderStatus.Delivered,  PaymentStatus=PaymentStatus.Paid,    SubTotal=1448m, ShippingCost=0m,  TaxAmount=260.64m, TotalAmount=1748m,     CreatedAt=_now.AddDays(-10), UpdatedAt=_now.AddDays(-5),  ShippedAt=_now.AddDays(-8), DeliveredAt=_now.AddDays(-5) },
            new Order { Id=2, OrderNumber="ZOV-000002", CustomerId=2, Status=OrderStatus.Processing, PaymentStatus=PaymentStatus.Paid,    SubTotal=999m,  ShippingCost=49m, TaxAmount=179.82m, TotalAmount=1227.82m,  CreatedAt=_now.AddDays(-3),  UpdatedAt=_now.AddDays(-3) },
            new Order { Id=3, OrderNumber="ZOV-000003", CustomerId=3, Status=OrderStatus.Shipped,    PaymentStatus=PaymentStatus.Paid,    SubTotal=299m,  ShippingCost=49m, TaxAmount=53.82m,  TotalAmount=401.82m,   CreatedAt=_now.AddDays(-2),  UpdatedAt=_now.AddDays(-1),  ShippedAt=_now.AddDays(-1) },
            new Order { Id=4, OrderNumber="ZOV-000004", CustomerId=4, Status=OrderStatus.Pending,    PaymentStatus=PaymentStatus.Pending, SubTotal=448m,  ShippingCost=49m, TaxAmount=80.64m,  TotalAmount=577.64m,   CreatedAt=_now.AddDays(-1),  UpdatedAt=_now.AddDays(-1) },
            new Order { Id=5, OrderNumber="ZOV-000005", CustomerId=1, Status=OrderStatus.Cancelled,  PaymentStatus=PaymentStatus.Refunded,SubTotal=1299m, ShippingCost=0m,  TaxAmount=233.82m, TotalAmount=1532.82m,  CreatedAt=_now.AddDays(-7),  UpdatedAt=_now.AddDays(-6) }
        );

        mb.Entity<OrderItem>().HasData(
            new OrderItem { Id=1, OrderId=1, ProductId=1, ProductName="Cotton T-Shirt",        ProductSKU="CLO-001", Quantity=1, UnitPrice=299m,  Discount=0m },
            new OrderItem { Id=2, OrderId=1, ProductId=3, ProductName="USB-C Hub 7-in-1",      ProductSKU="ELC-001", Quantity=1, UnitPrice=1499m, Discount=350m },
            new OrderItem { Id=3, OrderId=2, ProductId=4, ProductName="Wireless Earbuds",      ProductSKU="ELC-002", Quantity=1, UnitPrice=999m,  Discount=0m },
            new OrderItem { Id=4, OrderId=3, ProductId=1, ProductName="Cotton T-Shirt",        ProductSKU="CLO-001", Quantity=1, UnitPrice=299m,  Discount=0m },
            new OrderItem { Id=5, OrderId=4, ProductId=2, ProductName="Premium Green Tea",     ProductSKU="FNB-001", Quantity=2, UnitPrice=149m,  Discount=0m },
            new OrderItem { Id=6, OrderId=4, ProductId=5, ProductName="Yoga Mat Pro",          ProductSKU="SPT-001", Quantity=1, UnitPrice=150m,  Discount=0m },
            new OrderItem { Id=7, OrderId=5, ProductId=8, ProductName="Bluetooth Speaker",     ProductSKU="ELC-003", Quantity=1, UnitPrice=1299m, Discount=0m }
        );
    }
}
