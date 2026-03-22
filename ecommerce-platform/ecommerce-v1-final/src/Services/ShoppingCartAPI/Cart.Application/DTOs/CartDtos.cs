using Cart.Domain.Entities;

namespace Cart.Application.DTOs;

public sealed record CartDto(
    Guid CustomerId,
    IEnumerable<CartItemDto> Items,
    decimal Subtotal,
    string? AppliedCouponCode,
    decimal CouponDiscount,
    decimal Total,
    int ItemCount,
    DateTime LastModified)
{
    public static CartDto FromDomain(ShoppingCart cart) => new(
        cart.CustomerId,
        cart.Items.Select(i => new CartItemDto(
            i.ProductId, i.ProductName, i.Sku,
            i.UnitPrice, i.Quantity, i.LineTotal, i.ImageUrl)),
        cart.Subtotal, cart.AppliedCouponCode,
        cart.CouponDiscount, cart.Total, cart.ItemCount, cart.LastModified);
}

public sealed record CartItemDto(
    Guid ProductId, string ProductName, string Sku,
    decimal UnitPrice, int Quantity, decimal LineTotal, string? ImageUrl);
