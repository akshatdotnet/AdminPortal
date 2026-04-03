namespace STHEnterprise.Mvc.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Qty { get; set; }
        public string Image { get; set; }
    }

    public class Cart
    {
        public List<CartItem> Items { get; set; } = new();
    }


}
