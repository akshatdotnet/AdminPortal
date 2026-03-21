using Microsoft.AspNetCore.Authorization;
using AdminPortal.Application.Interfaces;
using AdminPortal.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Web.Controllers;

[Authorize]
public class PayoutsController : Controller
{
    private readonly IPayoutService _payoutService;

    public PayoutsController(IPayoutService payoutService)
    {
        _payoutService = payoutService;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _payoutService.GetPayoutSummaryAsync();
        return View(new PayoutsViewModel { Summary = result.Data! });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestPayout(decimal amount)
    {
        var result = await _payoutService.RequestPayoutAsync(amount);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Payout requested successfully!" : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }
}
