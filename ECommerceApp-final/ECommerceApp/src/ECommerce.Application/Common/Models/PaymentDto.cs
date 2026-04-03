namespace ECommerce.Application.Common.Models;

public record PaymentDto(
    Guid Id,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string Method,
    string Status,
    string? GatewayTransactionId,
    DateTime CreatedAt
);
