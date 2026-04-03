using Microsoft.AspNetCore.Mvc;
using STHEnterprise.Mvc.Models;


/*
 ✅ Final Corrected Controller Summary
You should end up with:
| Action                 | HTTP     | Purpose              |
| ---------------------- | -------- | -------------------- |
| Login()                | GET      | Show login page      |
| Login(LoginViewModel)  | POST     | Email/password login |
| LoginWithPhone(string) | POST     | Phone login          |
| Register()             | GET      | Register page        |
| ForgotPassword()       | GET/POST | Password recovery    |
| Logout()               | GET      | Logout               | 
 */

public class AccountController : Controller
{
    // ---------------------------
    // LOGIN
    // ---------------------------
    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // 🔹 MOCK USER CHECK
        if (model.Email == "admin@sth.com" && model.Password == "Admin@123")
        {
            TempData["UserName"] = "System Admin";
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError("", "Invalid email or password");
        return View(model);
    }

    
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // ---------------------------
    // FORGOT PASSWORD
    // ---------------------------
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // 🔹 Always success (security best practice)
        TempData["Success"] =
            "If the email exists, password reset instructions have been sent.";

        return RedirectToAction(nameof(ForgotPassword));
    }

    // ---------------------------
    // LOGOUT
    // ---------------------------
    //public IActionResult Logout()
    //{
    //    TempData.Clear();
    //    return RedirectToAction(nameof(Login));
    //}



    
    public IActionResult Index()
    {
        var user = HttpContext.Session.GetString(SessionKeys.USER);
        if (user == null)
            return RedirectToAction("Index", "Home");

        var vm = new AccountVM
        {
            Phone = user,
            Orders = new()
            {
                new() { Store="Suraj Tea House", OrderNo="#22475362", Amount=9135.32M, Status="Payment Failed", Date=DateTime.Now.AddDays(-1)},
                new() { Store="Beauty Accessories", OrderNo="#272100", Amount=30, Status="Delivered", Date=DateTime.Now.AddDays(-3)},
                new() { Store="Suraj Tea House", OrderNo="#3282", Amount=65, Status="Rejected", Date=DateTime.Now.AddDays(-10)}
            }
        };

        return View(vm);
    }

    [HttpPost]
    public IActionResult LoginWithPhone(string phone)
    {
        HttpContext.Session.SetString(SessionKeys.USER, phone);
        return Ok();
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Account");
    }

}
