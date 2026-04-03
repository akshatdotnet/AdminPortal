using Microsoft.AspNetCore.Mvc;
using STHEnterprise.Application.Interfaces;
using static STHEnterprise.Api.Models.Cart.RequestModels;

namespace STHEnterprise.Api.Controllers;

[ApiController]
[Route("api/v1/cart")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    // GET /api/v1/cart
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            success = true,
            data = _cartService.GetCart()
        });
    }

    // POST /api/v1/cart/items
    [HttpPost("items")]
    public IActionResult AddItem([FromBody] AddCartItemRequest request)
    {
        var cart = _cartService.AddItem(request.ProductId, request.Quantity);
        return Ok(new { success = true, data = cart });
    }

    // PUT /api/v1/cart/items/{productId}
    [HttpPut("items/{productId}")]
    public IActionResult UpdateItem(int productId, [FromBody] UpdateCartItemRequest request)
    {
        var cart = _cartService.UpdateItem(productId, request.Quantity);
        return Ok(new { success = true, data = cart });
    }

    // DELETE /api/v1/cart/items/{productId}
    [HttpDelete("items/{productId}")]
    public IActionResult RemoveItem(int productId)
    {
        _cartService.RemoveItem(productId);
        return Ok(new { success = true });
    }
}
