using STHEnterprise.Application.DTOs.Checkout;

namespace STHEnterprise.Application.Interfaces;

public interface ICheckoutService
{
    void SaveAddress(string userId, AddressDto address);
    OrderResponseDto ProcessPayment(string userId, PaymentRequestDto payment);
    OrderResponseDto ConfirmOrder(string userId);
}
