namespace ECommerce.Application.Common.Models;

public record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string Status,
    decimal TotalAmount,
    string Currency,
    List<OrderItemDto> Items,
    AddressDto ShippingAddress,
    DateTime CreatedAt,
    string? TrackingNumber
);

public record OrderItemDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal Subtotal);

public record AddressDto(string Street, string City, string State, string PinCode, string Country = "India");
