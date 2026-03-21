using AdminPortal.Application.DTOs;
using AdminPortal.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AdminPortal.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }

    // ── LOGIN ─────────────────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // Already authenticated → redirect to dashboard
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto dto, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(dto);

        var result = await _authService.LoginAsync(dto);

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            ViewBag.IsLockedOut = true;
            return View(dto);
        }

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            if (result.RemainingAttempts.HasValue)
                ViewBag.RemainingAttempts = result.RemainingAttempts.Value;
            return View(dto);
        }

        // Build cookie claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name,            result.UserName!),
            new(ClaimTypes.Email,           result.UserEmail!),
            new(ClaimTypes.Role,            result.UserRole!),
            new("FullName",                 result.UserName!),
        };

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = dto.RememberMe,
            ExpiresUtc   = dto.RememberMe
                ? DateTimeOffset.UtcNow.AddDays(30)
                : DateTimeOffset.UtcNow.AddHours(8),
            AllowRefresh = true
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);

        return RedirectToLocal(returnUrl);
    }

    // ── LOGOUT ────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    // ── FORGOT PASSWORD ───────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        await _authService.ForgotPasswordAsync(dto);

        // Always redirect — prevents email enumeration
        return RedirectToAction(nameof(ForgotPasswordConfirmation), new { email = dto.Email });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation(string email)
    {
        // Mask email for display: p****@gmail.com
        ViewBag.MaskedEmail = MaskEmail(email);
        return View();
    }

    // ── RESET PASSWORD ────────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(string token, string email)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            return RedirectToAction(nameof(ResetPasswordInvalid));

        var isValid = await _authService.ValidateResetTokenAsync(token, email);
        if (!isValid)
            return RedirectToAction(nameof(ResetPasswordInvalid));

        return View(new ResetPasswordDto { Token = token, Email = email });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var result = await _authService.ResetPasswordAsync(dto);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(dto);
        }

        TempData["ResetSuccess"] = "Your password has been reset. You can now log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordInvalid() => View();



    // ── REGISTER ──────────────────────────────────────────────────────
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var result = await _authService.RegisterAsync(dto);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(dto);
        }

        TempData["RegisterSuccess"] = "Account created successfully! Please log in.";
        return RedirectToAction(nameof(Login));
    }





    // ── ACCESS DENIED ─────────────────────────────────────────────────

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    // ── Helpers ───────────────────────────────────────────────────────

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Dashboard");
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return email;
        var parts = email.Split('@');
        if (parts.Length != 2) return email;
        var name   = parts[0];
        var domain = parts[1];
        var masked = name.Length <= 2
            ? new string('*', name.Length)
            : name[0] + new string('*', name.Length - 1);
        return $"{masked}@{domain}";
    }
}
