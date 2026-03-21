using Microsoft.AspNetCore.Authorization;
using AdminPortal.Application.Interfaces;
using AdminPortal.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IAnalyticsService _analyticsService;
    private readonly IOrderService _orderService;
    private readonly IStoreService _storeService;
    private readonly ICreditService _creditService;

    public DashboardController(
        IAnalyticsService analyticsService,
        IOrderService orderService,
        IStoreService storeService,
        ICreditService creditService)
    {
        _analyticsService = analyticsService;
        _orderService = orderService;
        _storeService = storeService;
        _creditService = creditService;
    }

    public async Task<IActionResult> Index(string period = "7d")
    {
        var (from, to) = GetDateRange(period);
        var analyticsResult = await _analyticsService.GetDashboardSummaryAsync(from, to);
        var ordersResult    = await _orderService.GetRecentOrdersAsync(8);
        var storeResult     = await _storeService.GetCurrentStoreAsync();
        var creditResult    = await _creditService.GetSummaryAsync();

        ViewBag.StoreName      = storeResult.Data?.StoreName ?? "Shopzo";
        ViewBag.StoreIsOpen    = storeResult.Data?.IsOpen ?? false;
        ViewBag.DukaanCredits  = creditResult.Data?.CurrentBalance.ToString("F2") ?? "0.00";

        return View(new DashboardViewModel
        {
            Analytics      = analyticsResult.Data!,
            RecentOrders   = ordersResult.Data?.ToList() ?? new(),
            StoreName      = storeResult.Data?.StoreName ?? "Shopzo",
            StoreIsOpen    = storeResult.Data?.IsOpen ?? false,
            SelectedPeriod = period
        });
    }

    private static (DateTime from, DateTime to) GetDateRange(string period) => period switch
    {
        "today" => (DateTime.UtcNow.Date, DateTime.UtcNow),
        "7d"    => (DateTime.UtcNow.AddDays(-7),  DateTime.UtcNow),
        "30d"   => (DateTime.UtcNow.AddDays(-30), DateTime.UtcNow),
        "90d"   => (DateTime.UtcNow.AddDays(-90), DateTime.UtcNow),
        _       => (DateTime.UtcNow.AddDays(-7),  DateTime.UtcNow)
    };
}
