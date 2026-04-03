namespace STHEnterprise.Application.DTOs.Checkout;

public class OrderResponseDto
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
}
