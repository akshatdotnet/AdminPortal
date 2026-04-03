using Microsoft.AspNetCore.Mvc;

public class ShopController : Controller
{
    // ================= MOCK DATA =================

    private static readonly List<Category> Categories = new()
    {
        new() { Id = 1, Name = "ClimaTech Enterprises", ProductCount = 1 },
        new() { Id = 2, Name = "Shreya Enterprises", ProductCount = 15 },
        new() { Id = 3, Name = "Eyebetes Enterprises", ProductCount = 39 },
        new() { Id = 4, Name = "Sunny Enterprises", ProductCount = 7 },
        new() { Id = 5, Name = "Catering Services", ProductCount = 53 }
    };

    private static readonly List<Product> Products = Enumerable.Range(1, 33)
        .Select(i => new Product
        {
            Id = i,
            CategoryId = i % 5 + 1,
            Name = $"Product {i}",
            Price = 50 + i * 10,
            MRP = 80 + i * 12,
            ImageUrl = "/images/sample-product.png"
        }).ToList();

    // ================= ACTIONS =================

    public IActionResult Index(int categoryId = 5)
    {
        ViewBag.Categories = Categories;
        ViewBag.Products = Products.Where(p => p.CategoryId == categoryId).ToList();
        ViewBag.SelectedCategory = categoryId;

        return View();
    }

    public IActionResult Checkout()
    {
        return View("Index"); // same view, different step
    }
}
