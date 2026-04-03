public class OrderDto
{
    public long OrderId { get; set; }
    public string StoreName { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public int ItemsCount { get; set; }
    public string OrderStatus { get; set; } = "";
    public DateTime OrderDate { get; set; }
}
