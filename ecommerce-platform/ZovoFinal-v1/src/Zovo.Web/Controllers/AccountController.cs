using Microsoft.AspNetCore.Mvc;
namespace Zovo.Web.Controllers;
public class AccountController : Controller
{
    public IActionResult ChangePassword() => View();
}
