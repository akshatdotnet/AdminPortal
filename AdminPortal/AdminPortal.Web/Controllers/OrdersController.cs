using Microsoft.AspNetCore.Authorization;
using AdminPortal.Application.Interfaces;
using AdminPortal.Domain.Entities;
using AdminPortal.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Web.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<IActionResult> Index(int page = 1, string? status = null)
    {
        const int pageSize = 15;
        OrderStatus? orderStatus = status switch
        {
            "Pending"    => OrderStatus.Pending,
            "Processing" => OrderStatus.Processing,
            "Shipped"    => OrderStatus.Shipped,
            "Delivered"  => OrderStatus.Delivered,
            "Cancelled"  => OrderStatus.Cancelled,
            _            => null
        };

        var result      = await _orderService.GetOrdersAsync(page, pageSize, orderStatus);
        var allResult   = await _orderService.GetOrdersAsync(1, 1000);
        var all         = allResult.Data?.Items.ToList() ?? new();

        return View(new OrdersViewModel
        {
            Orders           = result.Data!,
            SelectedStatus   = orderStatus,
            PendingCount     = all.Count(o => o.StatusEnum == OrderStatus.Pending),
            ProcessingCount  = all.Count(o => o.StatusEnum == OrderStatus.Processing),
            DeliveredCount   = all.Count(o => o.StatusEnum == OrderStatus.Delivered),
            CancelledCount   = all.Count(o => o.StatusEnum == OrderStatus.Cancelled),
        });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(Guid id, string status)
    {
        if (Enum.TryParse<OrderStatus>(status, out var orderStatus))
            await _orderService.UpdateOrderStatusAsync(id, orderStatus);
        TempData["Success"] = "Order status updated.";
        return RedirectToAction(nameof(Index));
    }
}
