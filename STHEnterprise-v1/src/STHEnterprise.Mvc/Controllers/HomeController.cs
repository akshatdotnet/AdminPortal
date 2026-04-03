using Microsoft.AspNetCore.Mvc;
using STHEnterprise.Mvc.Models;

namespace STHEnterprise.Mvc.Controllers
{
    public class HomeController : Controller
    {
        // =============================
        // DASHBOARD
        // =============================
        public IActionResult Index()
        {
            var model = new DashboardViewModel
            {
                TotalCustomers = 128,
                ActiveCustomers = 110,
                MonthlyRevenue = 245000,
                RecentActivities = new[]
                {
                    "Customer KVN created",
                    "Report generated",
                    "Profile updated"
                }
            };

            return View(model);
        }

        // =============================
        // CUSTOMERS
        // =============================
        public IActionResult Customers()
        {
            var customers = new List<CustomerViewModel>
            {
                new() { Id = 1, Name = "KVN", Email = "kvn@test.com", IsActive = true },
                new() { Id = 2, Name = "STH Corp", Email = "sth@test.com", IsActive = false },
                new() { Id = 3, Name = "VSV Ltd", Email = "vsv@test.com", IsActive = true }
            };

            return View(customers);
        }

        // =============================
        // REPORTS
        // =============================
        public IActionResult Reports()
        {
            var reports = new List<ReportViewModel>
            {
                new() { Title = "Monthly Sales", CreatedOn = DateTime.Today.AddDays(-2) },
                new() { Title = "Customer Growth", CreatedOn = DateTime.Today.AddDays(-7) }
            };

            return View(reports);
        }

        // =============================
        // PRIVACY
        // =============================
        public IActionResult Privacy()
        {
            return View();
        }


        // Mock user (replace with API later)
        private static ProfileViewModel MockUser = new()
        {
            FullName = "System Admin",
            Email = "admin@sth.com",
            Mobile = "9999999999"
        };

        // ==========================
        // PROFILE PAGE
        // ==========================
        public IActionResult Profile()
        {
            return View(MockUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Update mock user
            MockUser.FullName = model.FullName;
            MockUser.Mobile = model.Mobile;

            TempData["Success"] = "Profile updated successfully";
            return RedirectToAction(nameof(Profile));
        }

        // ==========================
        // CHANGE PASSWORD (MODAL)
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid password input";
                return RedirectToAction(nameof(Profile));
            }

            // Mock validation
            if (model.CurrentPassword != "Admin@123")
            {
                TempData["Error"] = "Current password is incorrect";
                return RedirectToAction(nameof(Profile));
            }

            TempData["Success"] = "Password changed successfully";
            return RedirectToAction(nameof(Profile));
        }
    }
}


//using System.Diagnostics;
//using Microsoft.AspNetCore.Mvc;
//using STHEnterprise.Mvc.Models;

//namespace STHEnterprise.Mvc.Controllers;

//public class HomeController : Controller
//{
//    private readonly ILogger<HomeController> _logger;

//    public HomeController(ILogger<HomeController> logger)
//    {
//        _logger = logger;
//    }

//    public IActionResult Index()
//    {
//        return View();
//    }

//    public IActionResult Privacy()
//    {
//        return View();
//    }

//    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
//    public IActionResult Error()
//    {
//        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
//    }
//}
