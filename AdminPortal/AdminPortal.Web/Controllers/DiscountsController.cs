using Microsoft.AspNetCore.Authorization;
using AdminPortal.Application.DTOs;
using AdminPortal.Application.Interfaces;
using AdminPortal.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Web.Controllers;

[Authorize]
public class DiscountsController : Controller
{
    private readonly IDiscountService _discountService;

    public DiscountsController(IDiscountService discountService)
    {
        _discountService = discountService;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _discountService.GetAllDiscountsAsync();
        var discounts = result.Data?.ToList() ?? new();

        return View(new DiscountsViewModel
        {
            Discounts = discounts,
            TotalActive = discounts.Count(d => d.IsActive && !d.IsExpired),
            TotalExpired = discounts.Count(d => d.IsExpired)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateDiscountDto dto)
    {
        var result = await _discountService.CreateDiscountAsync(dto);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? $"Discount code '{dto.Code}' created!" : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(Guid id)
    {
        await _discountService.ToggleDiscountAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _discountService.DeleteDiscountAsync(id);
        TempData["Success"] = "Discount removed.";
        return RedirectToAction(nameof(Index));
    }
}
