namespace Zovo.Core.Enums;

public enum OrderStatus
{
    Pending    = 0,
    Confirmed  = 1,
    Processing = 2,
    Shipped    = 3,
    Delivered  = 4,
    Returned   = 5,
    Cancelled  = 6
}

public enum PaymentStatus
{
    Pending           = 0,
    Paid              = 1,
    Failed            = 2,
    Refunded          = 3,
    PartiallyRefunded = 4
}

public enum CustomerStatus
{
    Active   = 0,
    Inactive = 1,
    Blocked  = 2
}
