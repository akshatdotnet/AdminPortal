using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WalletSystem.Data;
using WalletSystem.Services;
using WalletSystem.ViewModels;

namespace WalletSystem.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly AppDbContext _db;
    private readonly IAuthService _auth;

    public ProfileController(AppDbContext db, IAuthService auth)
    {
        _db   = db;
        _auth = auth;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET: /Profile
    public async Task<IActionResult> Index()
    {
        var user = await _db.Users
            .Include(u => u.Wallet)
            .ThenInclude(w => w!.Transactions)
            .FirstOrDefaultAsync(u => u.Id == CurrentUserId);

        if (user == null) return NotFound();

        var vm = new ProfileVM
        {
            Id             = user.Id,
            FullName       = user.FullName,
            Email          = user.Email,
            Username       = user.Username,
            PhoneNumber    = user.PhoneNumber,
            Bio            = user.Bio,
            AvatarUrl      = user.AvatarUrl,
            Address        = user.Address,
            City           = user.City,
            Country        = user.Country,
            DateOfBirth    = user.DateOfBirth,
            Role           = user.Role.ToString(),
            Status         = user.Status.ToString(),
            IsEmailVerified= user.IsEmailVerified,
            CreatedAt      = user.CreatedAt,
            LastLoginAt    = user.LastLoginAt,
            LastLoginIp    = user.LastLoginIp,
            WalletBalance  = user.Wallet?.Balance,
            WalletCurrency = user.Wallet?.Currency,
            TransactionCount = user.Wallet?.Transactions.Count ?? 0,
            Initials       = user.Initials,
        };
        return View(vm);
    }

    // GET: /Profile/Edit
    public async Task<IActionResult> Edit()
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound();

        return View(new EditProfileVM
        {
            Id          = user.Id,
            FullName    = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Bio         = user.Bio,
            AvatarUrl   = user.AvatarUrl,
            Address     = user.Address,
            City        = user.City,
            Country     = user.Country,
            DateOfBirth = user.DateOfBirth,
        });
    }

    // POST: /Profile/Edit
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProfileVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound();

        user.FullName    = vm.FullName;
        user.PhoneNumber = vm.PhoneNumber;
        user.Bio         = vm.Bio;
        user.AvatarUrl   = vm.AvatarUrl;
        user.Address     = vm.Address;
        user.City        = vm.City;
        user.Country     = vm.Country;
        user.DateOfBirth = vm.DateOfBirth;
        user.UpdatedAt   = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Refresh FullName claim in cookie
        await RefreshAuthCookieAsync(user);

        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Profile/ChangePassword
    public IActionResult ChangePassword() =>
        View(new ChangePasswordVM { UserId = CurrentUserId });

    // POST: /Profile/ChangePassword
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var ok = await _auth.ChangePasswordAsync(CurrentUserId, vm.CurrentPassword, vm.NewPassword);
        if (!ok)
        {
            ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
            return View(vm);
        }

        TempData["Success"] = "Password changed successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Profile/Security
    public async Task<IActionResult> Security()
    {
        var user = await _db.Users.FindAsync(CurrentUserId);
        if (user == null) return NotFound();
        ViewBag.User = user;
        return View();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    private async Task RefreshAuthCookieAsync(Models.User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name,           user.Username),
            new(ClaimTypes.Email,          user.Email),
            new("FullName",                user.FullName),
            new(ClaimTypes.Role,           user.Role.ToString()),
            new("WalletId",                user.Wallet?.Id.ToString() ?? ""),
            new("Initials",                user.Initials),
        };

        var identity   = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal  = new ClaimsPrincipal(identity);
        var existingProps = (await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme)).Properties
                           ?? new AuthenticationProperties();

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, existingProps);
    }
}
