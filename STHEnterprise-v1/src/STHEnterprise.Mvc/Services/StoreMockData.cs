using STHEnterprise.Mvc.Models;

public static class StoreMockData
{
    public static StoreViewModel Build()
    {

        return new StoreViewModel
        {
            StoreInfo = new StoreInfoVM
            {
                StoreName = "Suraj Tea House",
                Description = "Best Tea & Snacks"
            },

            Categories = Enumerable.Range(1, 5)
                .Select(i => new CategoryVM
                {
                    Id = i,
                    Name = $"Category {i}",
                    Count = 10,
                    Active = i == 1
                })
                .ToList(),

            Products = Enumerable.Range(1, 33)
                .Select(i => new ProductVM
                {
                    Id = i,
                    Name = $"Product {i}",
                    Price = i * 100,
                    OriginalPrice = i * 120,
                    Image = "/images/product.png",
                    CategoryId = (i % 5) + 1
                })
                .ToList()
        };

    }


    public static List<ProductVM> GetProducts()
    {
        return new List<ProductVM>
        {
            new()
            {
                Id = 1,
                Name = "Hindware Snowcrest Froid 24L Air Cooler",
                Price = 7590,
                OriginalPrice = 9490,
                Image = "/images/products/cooler.png",
                CategoryId = 1
            },
            new()
            {
                Id = 2,
                Name = "Bread Pakoda",
                Price = 70,
                OriginalPrice = 90,
                Image = "/images/products/pakoda.jpg",
                CategoryId = 7
            }
            // add remaining mock items here
        };
    }


    public static StoreViewModel Build2(int? categoryId = null)
    {
        var categories = new List<CategoryVM>
        {
            new() { Id = 1, Name = "ClimaTech Enterprises", Count = 1, ImageUrl="https://picsum.photos/300/200?1" },
            new() { Id = 2, Name = "Shreya Enterprises", Count = 15, ImageUrl="https://picsum.photos/300/200?2" },
            new() { Id = 3, Name = "Eyebetes Enterprises", Count = 39, ImageUrl="https://picsum.photos/300/200?3" },
            new() { Id = 4, Name = "Sunny Enterprises", Count = 7, ImageUrl="https://picsum.photos/300/200?4" },
            new() { Id = 5, Name = "Vaishno Enterprises", Count = 14, ImageUrl="https://picsum.photos/300/200?5" },
            new() { Id = 6, Name = "Ginger Enterprises", Count = 4, ImageUrl="https://picsum.photos/300/200?6" },
            new() { Id = 7, Name = "Catering Services", Count = 53, ImageUrl="https://picsum.photos/300/200?7" }
        };

        if (categoryId.HasValue)
            categories.ForEach(c => c.Active = c.Id == categoryId);

        var products = new List<ProductVM>
        {
            new() { Id=1, CategoryId=2, Name="Air Cooler 24L", Price=7590, OriginalPrice=9490, Image="https://picsum.photos/100?1"},
            new() { Id=2, CategoryId=7, Name="Bread Pakoda", Price=70, OriginalPrice=0, Image="https://picsum.photos/100?2"},
            new() { Id=3, CategoryId=7, Name="Catering Service", Price=600, OriginalPrice=1800, Image="https://picsum.photos/100?3"},
        };

        if (categoryId.HasValue)
            products = products.Where(p => p.CategoryId == categoryId).ToList();

        return new StoreViewModel
        {
            StoreInfo = new StoreInfoVM(),
            Categories = categories,
            Products = products
        };
    }


}
