namespace STHEnterprise.Application.DTOs.Checkout;

public class PaymentRequestDto
{
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "MOCK_RAZORPAY";
}
