using Microsoft.AspNetCore.Authorization;
using AdminPortal.Application.DTOs;
using AdminPortal.Application.Interfaces;
using AdminPortal.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Web.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly IStoreService _storeService;
    private readonly IStaffService _staffService;

    public SettingsController(IStoreService storeService, IStaffService staffService)
    {
        _storeService = storeService;
        _staffService = staffService;
    }

    public async Task<IActionResult> Index(string tab = "store-details")
    {
        var storeResult = await _storeService.GetCurrentStoreAsync();
        var staffResult = await _staffService.GetAllStaffAsync();

        return View(new SettingsViewModel
        {
            Store = storeResult.Data ?? new(),
            ActiveTab = tab,
            Staff = staffResult.Data?.ToList() ?? new()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStore(UpdateStoreDto dto)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Index));

        var result = await _storeService.UpdateStoreAsync(dto);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Store settings saved successfully!" : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var result = await _storeService.ToggleStoreStatusAsync(id);
        return Json(new { success = result.IsSuccess, isOpen = result.Data?.IsOpen });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InviteStaff(InviteStaffDto dto)
    {
        var result = await _staffService.InviteStaffAsync(dto);
        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Staff member invited!" : result.ErrorMessage;
        return RedirectToAction(nameof(Index), new { tab = "staff-accounts" });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveStaff(Guid id)
    {
        await _staffService.RemoveStaffAsync(id);
        TempData["Success"] = "Staff member removed.";
        return RedirectToAction(nameof(Index), new { tab = "staff-accounts" });
    }
}
