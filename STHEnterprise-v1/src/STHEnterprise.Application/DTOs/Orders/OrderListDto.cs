namespace STHEnterprise.Application.DTOs.Orders;

public class OrderListDto
{
    public Guid OrderId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
}
