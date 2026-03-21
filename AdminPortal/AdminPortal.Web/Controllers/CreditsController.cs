using Microsoft.AspNetCore.Authorization;
using AdminPortal.Application.Interfaces;
using AdminPortal.Domain.Entities;
using AdminPortal.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Web.Controllers;

[Authorize]
public class CreditsController : Controller
{
    private readonly ICreditService _creditService;

    public CreditsController(ICreditService creditService)
    {
        _creditService = creditService;
    }

    public async Task<IActionResult> Index(string filter = "All", string dateRange = "30d")
    {
        TransactionType? txType = filter switch
        {
            "Debit"  => TransactionType.Debit,
            "Credit" => TransactionType.Credit,
            _        => null
        };

        var result = await _creditService.GetSummaryAsync(txType, dateRange);
        return View(new CreditsViewModel
        {
            Summary           = result.Data!,
            SelectedFilter    = filter,
            SelectedDateRange = dateRange
        });
    }
}
