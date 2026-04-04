using Microsoft.AspNetCore.Mvc;
namespace Zovo.Web.Controllers;
public class StoreController : Controller
{
    public IActionResult Index() => View();
}
