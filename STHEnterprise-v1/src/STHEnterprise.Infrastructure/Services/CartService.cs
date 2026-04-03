using STHEnterprise.Application.DTOs;
using STHEnterprise.Application.Interfaces;
//using STHEnterprise.Api.Extensions;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Text;

namespace STHEnterprise.Infrastructure.Services;

public class CartService : ICartService
{
    private const string CART_KEY = "CART";
    private readonly IHttpContextAccessor _http;

    public CartService(IHttpContextAccessor http)
    {
        _http = http;
    }

    private ISession Session => _http.HttpContext!.Session;

    public CartDto GetCart()
    {
        return Session.GetObject<CartDto>(CART_KEY) ?? new CartDto();
    }

    public CartDto AddItem(int productId, int quantity)
    {
        var cart = GetCart();

        var item = cart.Items.FirstOrDefault(x => x.ProductId == productId);

        if (item == null)
        {
            cart.Items.Add(new CartItemDto
            {
                ProductId = productId,
                Name = GetMockProductName(productId),
                Price = GetMockPrice(productId),
                Quantity = quantity,
                ImageUrl = GetMockImage(productId)
            });
        }
        else
        {
            item.Quantity += quantity;
        }

        Session.SetObject(CART_KEY, cart);
        return cart;
    }

    public CartDto UpdateItem(int productId, int quantity)
    {
        var cart = GetCart();
        var item = cart.Items.FirstOrDefault(x => x.ProductId == productId);

        if (item != null)
        {
            if (quantity <= 0)
                cart.Items.Remove(item);
            else
                item.Quantity = quantity;
        }

        Session.SetObject(CART_KEY, cart);
        return cart;
    }

    public void RemoveItem(int productId)
    {
        var cart = GetCart();
        cart.Items.RemoveAll(x => x.ProductId == productId);
        Session.SetObject(CART_KEY, cart);
    }

    public void Clear()
    {
        Session.Remove(CART_KEY);
    }

    // 🔧 Mock helpers
    private string GetMockProductName(int id) =>
        id switch
        {
            1 => "Ginger Tea",
            2 => "Masala Chai",
            _ => "Premium Tea"
        };

    private decimal GetMockPrice(int id) =>
        id switch
        {
            1 => 120,
            2 => 90,
            _ => 150
        };

    private string GetMockImage(int id) =>
        "https://images.unsplash.com/photo-1544787219-7f47ccb76574";
}

// Extension methods for ISession to handle object serialization/deserialization
public static class SessionExtensions
{
    public static void SetObject<T>(this ISession session, string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        var bytes = Encoding.UTF8.GetBytes(json); // Convert JSON string to byte array
        session.Set(key, bytes); // Use ISession.Set method
    }

    public static T? GetObject<T>(this ISession session, string key)
    {
        if (session.TryGetValue(key, out var bytes)) // Retrieve byte array from session
        {
            var json = Encoding.UTF8.GetString(bytes); // Convert byte array back to JSON string
            return JsonSerializer.Deserialize<T>(json); // Deserialize JSON string to object
        }
        return default;
    }
}
