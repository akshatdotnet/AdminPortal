namespace Payment.Application.DTOs;

public sealed record PaymentSessionDto(
    Guid PaymentId, string CheckoutUrl,
    string SessionId, string PaymentIntentId);
