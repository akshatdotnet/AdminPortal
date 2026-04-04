using Microsoft.AspNetCore.Mvc;
using Zovo.Application.Dashboard;

namespace Zovo.Web.Controllers;

public class AnalyticsController : Controller
{
    private readonly IDashboardService _svc;
    public AnalyticsController(IDashboardService svc) => _svc = svc;

    public async Task<IActionResult> Index()
    {
        var summary = await _svc.GetSummaryAsync();
        ViewBag.Monthly    = summary.MonthlySales;
        ViewBag.Categories = summary.CategoryBreakdown;
        ViewBag.Orders     = summary.TotalOrders;
        ViewBag.Revenue    = summary.TotalRevenue;
        return View();
    }
}
