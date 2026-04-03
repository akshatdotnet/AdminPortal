namespace STHEnterprise.Application.DTOs;

public class CartDto
{
    public List<CartItemDto> Items { get; set; } = new();

    public decimal SubTotal => Items.Sum(x => x.Total);
    public decimal Tax => Math.Round(SubTotal * 0.18m, 2);
    public decimal GrandTotal => SubTotal + Tax;
}
