using Microsoft.AspNetCore.Mvc;
using STHEnterprise.Mvc.Models;

namespace STHEnterprise.Mvc.Controllers;

[Route("categories")]
public class CategoryController : Controller
{
    //[HttpGet("")]
    //public IActionResult Index()
    //{
    //    var vm = StoreMockData.Build();
    //    return View(vm);
    //}
    
        public IActionResult Index()
        {
            ViewBag.ShowPolicyBar = true; // toggle header bar ON/OFF

            var model = new List<CategoryVM>
        {
            new() { Id = 1, Name = "ClimaTech Enterprises", ImageUrl = "https://picsum.photos/400/300?1", Slug = "climatech" },
            new() { Id = 2, Name = "Shreya Enterprises", ImageUrl = "https://picsum.photos/400/300?2", Slug = "shreya-enterprises" },
            new() { Id = 3, Name = "Eyebetes Enterprises", ImageUrl = "https://picsum.photos/400/300?3", Slug = "eyebetes" },
            new() { Id = 4, Name = "Sunny Enterprises", ImageUrl = "https://picsum.photos/400/300?4", Slug = "sunny" },
            new() { Id = 5, Name = "Vaishno Enterprises", ImageUrl = "https://picsum.photos/400/300?5", Slug = "vaishno" },
            new() { Id = 6, Name = "Catering Services", ImageUrl = "https://picsum.photos/400/300?6", Slug = "catering-services" }
        };

            return View(model);
        }
    


    [HttpGet("{slug}")]
    public IActionResult Details(string slug)
    {
        var categoryId = slug switch
        {
            "climatech-enterprises" => 1,
            "shreya-enterprises" => 2,
            "eyebetes-enterprises" => 3,
            "sunny-enterprises" => 4,
            "vaishno-enterprises" => 5,
            "ginger-enterprises" => 6,
            "catering-services" => 7,
            _ => 0
        };

        var vm = StoreMockData.Build2(categoryId);

        return View("Category", vm);
    }
}
