using STHEnterprise.Application.DTOs.Checkout;
using STHEnterprise.Application.Interfaces;
using STHEnterprise.Core.Entities;

namespace STHEnterprise.Infrastructure.Services;

public class CheckoutService : ICheckoutService
{
    private static readonly Dictionary<string, Address> AddressStore = new();
    private static readonly Dictionary<string, Order> OrderStore = new();

    public void SaveAddress(string userId, AddressDto dto)
    {
        AddressStore[userId] = new Address
        {
            FullName = dto.FullName,
            Phone = dto.Phone,
            Line1 = dto.Line1,
            City = dto.City,
            State = dto.State,
            Pincode = dto.Pincode
        };
    }

    public OrderResponseDto ProcessPayment(string userId, PaymentRequestDto payment)
    {
        var order = new Order
        {
            UserId = userId,
            TotalAmount = payment.Amount,
            PaymentStatus = "Paid"
        };

        OrderStore[userId] = order;

        return new OrderResponseDto
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Status = order.PaymentStatus
        };
    }

    public OrderResponseDto ConfirmOrder(string userId)
    {
        var order = OrderStore[userId];

        return new OrderResponseDto
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Status = "Confirmed"
        };
    }
}
