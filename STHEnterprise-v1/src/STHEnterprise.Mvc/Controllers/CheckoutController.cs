using Microsoft.AspNetCore.Mvc;

public class CheckoutController : Controller
{

    private const decimal GST_RATE = 0.18m;
    private const decimal PROFESSIONAL_TAX_RATE = 0.02m;


    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult PlaceOrder()
    {
        TempData["Success"] = "Order placed successfully!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult ApplyCoupon(string couponCode, decimal itemTotal)
    {
        decimal discount = 0;

        if (couponCode == "SAVE500")
            discount = 500;
        else if (couponCode == "SAVE10")
            discount = itemTotal * 0.10m;
        else
            return BadRequest("Invalid coupon");

        return Json(new { discount });
    }

    [HttpPost]
    public IActionResult CalculateTotals(decimal itemTotal, decimal discount)
    {
        var gst = (itemTotal - discount) * GST_RATE;
        var professionalTax = (itemTotal - discount) * PROFESSIONAL_TAX_RATE;
        var delivery = itemTotal > 500 ? 0 : 50;

        var grandTotal = itemTotal - discount + gst + professionalTax + delivery;

        return Json(new
        {
            itemTotal,
            discount,
            gst,
            professionalTax,
            delivery,
            grandTotal
        });
    }

    public IActionResult Address()
    {
        return View();
    }
    public IActionResult Info()
    {
        return View();
    }

    public IActionResult Payment()
    {
        return View();
    }
    public IActionResult Success()
    {
        return View();
    }


}
