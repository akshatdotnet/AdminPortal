using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WalletSystem.Services;
using WalletSystem.ViewModels;

namespace WalletSystem.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    // ── Login ──────────────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new LoginVM { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var (ok, msg, user) = await _auth.ValidateLoginAsync(vm.Identifier, vm.Password, ip);

        if (!ok)
        {
            ModelState.AddModelError("", msg);
            return View(vm);
        }

        // Build claims principal
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user!.Id.ToString()),
            new(ClaimTypes.Name,           user.Username),
            new(ClaimTypes.Email,          user.Email),
            new("FullName",                user.FullName),
            new(ClaimTypes.Role,           user.Role.ToString()),
            new("WalletId",                user.Wallet?.Id.ToString() ?? ""),
            new("Initials",                user.Initials),
        };

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProps = new AuthenticationProperties
        {
            IsPersistent = vm.RememberMe,
            ExpiresUtc   = vm.RememberMe
                ? DateTimeOffset.UtcNow.AddDays(30)
                : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

        TempData["Success"] = $"Welcome back, {user.FullName}!";

        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            return Redirect(vm.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    // ── Logout ─────────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "You have been signed out.";
        return RedirectToAction(nameof(Login));
    }

    // ── Forgot Password ────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordVM());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var (ok, token) = await _auth.GeneratePasswordResetTokenAsync(vm.Email);

        // Always show success to prevent email enumeration
        TempData["ResetEmail"] = vm.Email;
        if (ok)
        {
            // In production: send email with reset link
            // For demo: store token in TempData so user can click the link directly
            TempData["ResetToken"] = token;
            TempData["ResetLink"]  = Url.Action("ResetPassword", "Auth",
                new { email = vm.Email, token }, Request.Scheme);
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    // ── Reset Password ─────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            TempData["Error"] = "Invalid password reset link.";
            return RedirectToAction(nameof(Login));
        }
        return View(new ResetPasswordVM { Email = email, Token = token });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var (ok, msg) = await _auth.ResetPasswordAsync(vm.Email, vm.Token, vm.NewPassword);
        if (!ok)
        {
            ModelState.AddModelError("", msg);
            return View(vm);
        }

        TempData["Success"] = "Password reset successfully. Please log in with your new password.";
        return RedirectToAction(nameof(Login));
    }

    // ── Access Denied ──────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult AccessDenied() => View();
}
