namespace STHEnterprise.Mvc.Models;

public class CartItemVM
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string ImageUrl { get; set; } = string.Empty;

    public decimal Total => Price * Quantity;
}

public class CartVM
{
    public List<CartItemVM> Items { get; set; } = new();

    public decimal SubTotal => Items.Sum(x => x.Total);
    public decimal Tax => Math.Round(SubTotal * 0.18m, 2);
    public decimal GrandTotal => SubTotal + Tax;
}


