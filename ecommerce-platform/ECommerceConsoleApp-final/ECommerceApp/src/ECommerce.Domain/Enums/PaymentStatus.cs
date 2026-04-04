namespace ECommerce.Domain.Enums;

public enum PaymentStatus
{
    Pending = 1,
    Processing = 2,
    Captured = 3,
    Failed = 4,
    Refunded = 5,
    PartiallyRefunded = 6
}
