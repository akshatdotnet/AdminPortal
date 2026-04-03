using Microsoft.AspNetCore.Mvc;
using Zovo.Application.Orders;
using Zovo.Core.Enums;
using static Zovo.Application.Orders.OrderDtos;

namespace Zovo.Web.Controllers;

public class OrdersController : Controller
{
    private readonly IOrderService _svc;
    public OrdersController(IOrderService svc) => _svc = svc;

    // GET /Orders
    public async Task<IActionResult> Index(
        string? search, string? status, string? payStatus,
        string sortBy = "newest", int page = 1)
    {
        var q = new OrderQueryParams {
            Search = search, Status = status,
            PaymentStatus = payStatus, SortBy = sortBy, Page = page
        };
        var result = await _svc.GetPagedAsync(q);
        ViewData["Search"]    = search;
        ViewData["Status"]    = status;
        ViewData["PayStatus"] = payStatus;
        ViewData["SortBy"]    = sortBy;
        return View(result);
    }

    // GET /Orders/Detail/5
    public async Task<IActionResult> Detail(int id)
    {
        var dto = await _svc.GetDetailAsync(id);
        if (dto is null) return NotFound();
        return View(dto);
    }

    // POST /Orders/UpdateStatus  (AJAX)
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        if (!Enum.TryParse<OrderStatus>(status, out var s))
            return Json(new { success = false, message = "Invalid status." });
        var result = await _svc.UpdateStatusAsync(id, s);
        return Json(new { success = result.IsSuccess, message = result.Message });
    }

    // POST /Orders/Cancel/5  (AJAX)
    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _svc.CancelAsync(id);
        return Json(new { success = result.IsSuccess, message = result.Message });
    }


    /*

    // ── GET /Orders/Create ───────────────────────────────────────────────────
    public IActionResult Create() => View(new CreateOrderDto());

    // ── POST /Orders/Create ──────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderDto dto)
    {
        if (!ModelState.IsValid) return View(dto);

        var result = await _svc.CreateAsync(dto);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError("", result.Message);
            return View(dto);
        }

        TempData["Alert"] = $"success|{result.Message}";
        return RedirectToAction(nameof(Index));
    }

    */

}
