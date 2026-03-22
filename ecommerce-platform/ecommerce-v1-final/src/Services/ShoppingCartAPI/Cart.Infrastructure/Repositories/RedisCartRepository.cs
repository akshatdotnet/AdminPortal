using Cart.Application.Interfaces;
using Cart.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cart.Infrastructure.Repositories;

public sealed class RedisCartRepository(IDistributedCache cache) : ICartRepository
{
    private static readonly TimeSpan CartExpiry = TimeSpan.FromDays(30);
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    private static string Key(Guid id) => $"cart:{id}";

    public async Task<ShoppingCart?> GetAsync(Guid customerId, CancellationToken ct = default)
    {
        var json = await cache.GetStringAsync(Key(customerId), ct);
        if (string.IsNullOrEmpty(json)) return null;
        var dto = JsonSerializer.Deserialize<CartData>(json, JsonOpts);
        return dto?.ToCart();
    }

    public async Task<ShoppingCart> GetOrCreateAsync(Guid customerId, CancellationToken ct = default)
        => await GetAsync(customerId, ct) ?? ShoppingCart.Create(customerId);

    public async Task SaveAsync(ShoppingCart cart, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(CartData.From(cart), JsonOpts);
        await cache.SetStringAsync(Key(cart.CustomerId), json,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CartExpiry
            }, ct);
    }

    public async Task DeleteAsync(Guid customerId, CancellationToken ct = default)
        => await cache.RemoveAsync(Key(customerId), ct);
}

// Serialization DTO (avoids private-setter reflection issues)
public sealed class CartData
{
    public Guid CustomerId { get; set; }
    public List<ItemData> Items { get; set; } = new();
    public string? CouponCode { get; set; }
    public decimal CouponDiscount { get; set; }

    public sealed class ItemData
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = default!;
        public string Sku { get; set; } = default!;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
    }

    public static CartData From(ShoppingCart c) => new()
    {
        CustomerId = c.CustomerId,
        CouponCode = c.AppliedCouponCode,
        CouponDiscount = c.CouponDiscount,
        Items = c.Items.Select(i => new ItemData
        {
            ProductId = i.ProductId, ProductName = i.ProductName,
            Sku = i.Sku, UnitPrice = i.UnitPrice,
            Quantity = i.Quantity, ImageUrl = i.ImageUrl
        }).ToList()
    };

    public ShoppingCart ToCart()
    {
        var cart = ShoppingCart.Create(CustomerId);
        foreach (var i in Items)
            cart.AddItem(i.ProductId, i.ProductName, i.Sku, i.UnitPrice, i.Quantity, i.ImageUrl);
        if (!string.IsNullOrEmpty(CouponCode))
            cart.ApplyCoupon(CouponCode, CouponDiscount);
        return cart;
    }
}
