public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int ProductCount { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public decimal MRP { get; set; }
    public string ImageUrl { get; set; } = "";
    public int DiscountPercent =>
        MRP > Price ? (int)((MRP - Price) / MRP * 100) : 0;
}

public class CartItem
{
    public int ProductId { get; set; }
    public int Qty { get; set; }
}
