using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Persistence;

/// <summary>Seeds the database with sample data on first run.</summary>
public class DataSeeder(AppDbContext db, ILogger<DataSeeder> logger)
{
    public async Task SeedAsync()
    {
        await db.Database.EnsureCreatedAsync();

        if (await db.Categories.AnyAsync()) return;

        logger.LogInformation("Seeding database with initial data...");

        // Categories
        var electronics = Category.Create("Electronics", "Gadgets and devices");
        var clothing    = Category.Create("Clothing", "Fashion and apparel");
        var books       = Category.Create("Books", "Educational and fiction books");
        var home        = Category.Create("Home & Kitchen", "Home appliances and kitchen");

        await db.Categories.AddRangeAsync(electronics, clothing, books, home);
        await db.SaveChangesAsync();

        // Products
        var products = new[]
        {
            Product.Create("iPhone 15 Pro",       "Apple iPhone 15 Pro 256GB",     89999, 50, electronics.Id),
            Product.Create("Samsung Galaxy S24",   "Samsung Galaxy S24 128GB",      74999, 30, electronics.Id),
            Product.Create("OnePlus 12",           "OnePlus 12 5G 256GB",           64999, 40, electronics.Id),
            Product.Create("Sony WH-1000XM5",      "Noise Cancelling Headphones",   29999, 100,electronics.Id),
            Product.Create("MacBook Air M3",       "Apple MacBook Air 13\" M3",    114999, 20, electronics.Id),
            Product.Create("Nike Air Max 270",     "Running shoes, size 8-12",       8999, 200,clothing.Id),
            Product.Create("Levi's 511 Jeans",    "Slim fit denim, blue",           3999, 150,clothing.Id),
            Product.Create("Allen Solly Shirt",   "Formal cotton shirt",            1999, 300,clothing.Id),
            Product.Create("Clean Code",           "Robert C. Martin",               699, 500, books.Id),
            Product.Create("The Pragmatic Programmer","D. Thomas & A. Hunt",         799, 400, books.Id),
            Product.Create("Design Patterns",      "Gang of Four",                   899, 350, books.Id),
            Product.Create("Instant Pot Duo",      "7-in-1 Electric Pressure Cooker",7999, 80, home.Id),
            Product.Create("Philips Air Fryer",    "4.1L HD9200",                   4999, 120,home.Id),
        };

        await db.Products.AddRangeAsync(products);

        // Demo customers
        var customers = new[]
        {
            Customer.Create("Rahul", "Sharma",  "rahul.sharma@example.com",  "+91-9876543210"),
            Customer.Create("Priya", "Patel",   "priya.patel@example.com",   "+91-9876543211"),
            Customer.Create("Amit",  "Kumar",   "amit.kumar@example.com",    "+91-9876543212"),
        };

        await db.Customers.AddRangeAsync(customers);
        await db.SaveChangesAsync();

        logger.LogInformation("Seeding complete. {Products} products, {Customers} customers.",
            products.Length, customers.Length);
    }
}
