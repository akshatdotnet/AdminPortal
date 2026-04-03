using System.ComponentModel.DataAnnotations;
using Zovo.Core.Enums;
using Zovo.Core.ValueObjects;

namespace Zovo.Application.Orders;

public record OrderListItemDto(
    int Id, string OrderNumber, string CustomerName, string CustomerEmail,
    decimal TotalAmount, string Status, string PaymentStatus, DateTime CreatedAt);

public record OrderDetailDto(
    int Id, string OrderNumber, int CustomerId,
    string CustomerName, string CustomerEmail, string? CustomerPhone,
    decimal SubTotal, decimal ShippingCost, decimal Discount,
    decimal TaxAmount, decimal TotalAmount,
    string Status, string PaymentStatus,
    string? TrackingNumber, string? Notes, string? CouponCode,
    DateTime CreatedAt, DateTime? ShippedAt, DateTime? DeliveredAt,
    IEnumerable<OrderItemDto> Items);

public record OrderItemDto(
    int ProductId, string ProductName, string? SKU,
    int Quantity, decimal UnitPrice, decimal Discount, decimal LineTotal);

public class CreateOrderCommand
{
    [Required] public int CustomerId { get; set; }
    public int? ShippingAddressId { get; set; }
    [Required] public List<OrderLineInput> Lines { get; set; } = new();
    public string? CouponCode { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
}

public class OrderLineInput
{
    public int ProductId { get; set; }
    [Range(1, 9999)] public int Quantity { get; set; }
}

public class OrderQueryParams
{
    public string?   Search       { get; set; }
    public string?   Status       { get; set; }
    public string?   PaymentStatus { get; set; }
    public DateTime? From         { get; set; }
    public DateTime? To           { get; set; }
    public string    SortBy       { get; set; } = "newest";
    public int       Page         { get; set; } = 1;
    public int       PageSize     { get; set; } = 20;
}

public interface IOrderService
{
    Task<PagedResult<OrderListItemDto>> GetPagedAsync(OrderQueryParams q);
    Task<OrderDetailDto?> GetDetailAsync(int id);
    Task<Result<int>> CreateAsync(CreateOrderCommand cmd);
    Task<Result> UpdateStatusAsync(int id, OrderStatus status);
    Task<Result> UpdatePaymentStatusAsync(int id, PaymentStatus status);
    Task<Result> CancelAsync(int id);
}
