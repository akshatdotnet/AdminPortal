using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STHEnterprise.Application.DTOs.Checkout;
using STHEnterprise.Application.Interfaces;
using System.Security.Claims;

namespace STHEnterprise.Api.Controllers;

[ApiController]
[Route("api/v1/checkout")]
[Authorize]
public class CheckoutController : ControllerBase
{
    private readonly ICheckoutService _checkout;

    public CheckoutController(ICheckoutService checkout)
    {
        _checkout = checkout;
    }

    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // STEP 1: Address
    [HttpPost("address")]
    public IActionResult SaveAddress([FromBody] AddressDto address)
    {
        _checkout.SaveAddress(UserId, address);
        return Ok(new { success = true, step = "Address Saved" });
    }

    // STEP 2: Payment
    [HttpPost("payment")]
    public IActionResult Payment([FromBody] PaymentRequestDto payment)
    {
        var result = _checkout.ProcessPayment(UserId, payment);
        return Ok(result);
    }

    // STEP 3: Confirm
    [HttpPost("confirm")]
    public IActionResult Confirm()
    {
        var result = _checkout.ConfirmOrder(UserId);
        return Ok(result);
    }
}
