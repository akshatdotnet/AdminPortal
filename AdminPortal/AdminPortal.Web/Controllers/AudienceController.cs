using Microsoft.AspNetCore.Authorization;
using AdminPortal.Application.DTOs;
using AdminPortal.Application.Interfaces;
using AdminPortal.Domain.Entities;
using AdminPortal.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Web.Controllers;

[Authorize]
public class AudienceController : Controller
{
    private readonly ICustomerService _customerService;

    public AudienceController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public async Task<IActionResult> Index(int page = 1, string? type = null, string? search = null)
    {
        const int pageSize = 20;
        CustomerType? customerType = type switch
        {
            "New"       => CustomerType.New,
            "Returning" => CustomerType.Returning,
            "Imported"  => CustomerType.Imported,
            _           => null
        };

        var result = await _customerService.GetCustomersAsync(page, pageSize, customerType, search);
        var allResult = await _customerService.GetCustomersAsync(1, 1000);
        var all = allResult.Data?.Items.ToList() ?? new();

        return View(new AudienceViewModel
        {
            Customers     = result.Data!,
            SearchQuery   = search,
            SelectedType  = customerType,
            AllCount      = all.Count,
            NewCount      = all.Count(c => c.TypeEnum == CustomerType.New),
            ReturningCount= all.Count(c => c.TypeEnum == CustomerType.Returning),
            ImportedCount = all.Count(c => c.TypeEnum == CustomerType.Imported),
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCustomerDto dto)
    {
        var result = await _customerService.CreateCustomerAsync(dto);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? $"Customer '{dto.Name}' added!" : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }
}
