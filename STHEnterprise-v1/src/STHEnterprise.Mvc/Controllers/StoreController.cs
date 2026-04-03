using Microsoft.AspNetCore.Mvc;
using STHEnterprise.Mvc.Models;
using System.Collections.Generic;
using System.Linq;

public class StoreController : Controller
{

    public IActionResult Index1()
    {
        var vm = StoreMockData.Build();
        return View(vm);
    }

    public IActionResult Index()
    {
        var model = new StoreViewModel
        {
            StoreInfo = new StoreInfoVM(),

            Categories = new List<CategoryVM>
            {
                new() { Id = 1, Name = "ClimaTech Enterprises", Count = 1, Active = true },
                new() { Id = 2, Name = "Shreya Enterprises", Count = 15 },
                new() { Id = 3, Name = "Eyebetes Enterprises", Count = 39 },
                new() { Id = 4, Name = "Sunny Enterprises", Count = 7 },
                new() { Id = 5, Name = "Vaishno Enterprises", Count = 14 },
                new() { Id = 6, Name = "Ginger Enterprises", Count = 4 },
                new() { Id = 7, Name = "Catering Services", Count = 53 }
            },

            Products = StoreMockData.GetProducts()
        };

        return View(model);
    }

    public IActionResult Categories()
    {
        return View(); // same view, different step
    }

    
}
