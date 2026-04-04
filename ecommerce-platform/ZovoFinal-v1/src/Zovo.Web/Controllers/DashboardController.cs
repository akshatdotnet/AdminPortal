using Microsoft.AspNetCore.Mvc;
using Zovo.Application.Dashboard;

namespace Zovo.Web.Controllers;

public class DashboardController : Controller
{
    private readonly IDashboardService _svc;
    public DashboardController(IDashboardService svc) => _svc = svc;

    public async Task<IActionResult> Index()
    {
        var vm = await _svc.GetSummaryAsync();
        ViewData["PendingOrders"] = vm.PendingOrders;
        return View(vm);
    }
}
