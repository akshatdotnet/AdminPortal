namespace Order.Application.DTOs;

public sealed record PlaceOrderResponse(Guid OrderId, string OrderNumber, decimal Total);

public sealed record OrderDto(
    Guid Id, string OrderNumber, Guid CustomerId,
    string Status, string PaymentStatus,
    ShippingAddressDto ShippingAddress,
    IEnumerable<OrderItemDto> Items,
    decimal Subtotal, decimal DiscountAmount,
    decimal ShippingCost, decimal TaxAmount, decimal Total,
    string? CouponCode, string? TrackingNumber,
    string? CancellationReason, string? Notes,
    DateTime CreatedAt, DateTime? PaidAt,
    DateTime? ShippedAt, DateTime? DeliveredAt,
    IEnumerable<StatusHistoryDto> StatusHistory);

public sealed record OrderItemDto(
    Guid ProductId, string ProductName, string Sku,
    decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record ShippingAddressDto(
    string FullName, string Street, string City,
    string State, string PostalCode, string Country, string Phone);

public sealed record StatusHistoryDto(string Status, string Note, DateTime Timestamp);

public sealed record OrderSummaryDto(
    Guid Id, string OrderNumber, string Status,
    int ItemCount, decimal Total, DateTime CreatedAt);

public sealed class PagedResult<T>
{
    public IEnumerable<T> Items       { get; init; } = Enumerable.Empty<T>();
    public int TotalCount  { get; init; }
    public int PageNumber  { get; init; }
    public int PageSize    { get; init; }
    public int TotalPages  => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage     => PageNumber < TotalPages;

    public static PagedResult<T> Create(
        IEnumerable<T> items, int total, int page, int size) =>
        new() { Items = items, TotalCount = total, PageNumber = page, PageSize = size };
}
