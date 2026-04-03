namespace STHEnterprise.Application.DTOs.Orders;

public class OrderDetailDto
{
    public Guid OrderId { get; set; }
    public DateTime Date { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public List<OrderItemDto> Items { get; set; } = new();
}
