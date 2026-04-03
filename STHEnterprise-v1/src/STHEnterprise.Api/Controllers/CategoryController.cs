using Microsoft.AspNetCore.Mvc;

namespace STHEnterprise.Api.Controllers
{
    public class CategoryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
