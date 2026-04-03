namespace ECommerce.Application.Common.Models;

public record CartDto(
    Guid Id,
    Guid CustomerId,
    List<CartItemDto> Items,
    decimal TotalAmount,
    string Currency
);

public record CartItemDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal Subtotal);
