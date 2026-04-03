using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Filters;

namespace UserManagement.Controllers
{
    [RequireLogin]
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var stats = new
            {
                TotalUsers   = await _db.Users.CountAsync(),
                ActiveUsers  = await _db.Users.CountAsync(u => u.IsActive),
                TotalRoles   = await _db.Roles.CountAsync(),
                TotalModules = await _db.Modules.CountAsync()
            };
            return View(stats);
        }
    }
}
