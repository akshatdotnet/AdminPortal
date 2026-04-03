namespace ECommerce.Domain.Enums;

public enum CancellationReason
{
    CustomerRequest = 1,
    OutOfStock = 2,
    PaymentFailed = 3,
    FraudDetected = 4,
    PricingError = 5,
    Other = 99
}
