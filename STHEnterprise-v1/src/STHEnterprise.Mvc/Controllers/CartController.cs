using Microsoft.AspNetCore.Mvc;
//using STHEnterprise.Mvc.Extensions;
using STHEnterprise.Mvc.Models;

public class CartController : Controller
{
    private const string CART_KEY = "CART";

    public IActionResult Index()
    {
        // Try get cart from session
        var cart = HttpContext.Session.GetObject<CartVM>(CART_KEY);

        // ✅ FIX 1: Initialize cart if null
        if (cart == null)
        {
            cart = new CartVM();
        }

        // ✅ FIX 2: Auto-load mock data if empty
        if (!cart.Items.Any())
        {
            cart.Items.AddRange(new List<CartItemVM>
        {
            new CartItemVM
            {
                ProductId = 101,
                Name = "Hindware Snowcrest 24L Air Cooler",
                Price = 7590,
                Quantity = 1,
                ImageUrl = "https://images.unsplash.com/photo-1581574209760-5b29e45f7c6a"
            },
            new CartItemVM
            {
                ProductId = 102,
                Name = "Bread Pakoda",
                Price = 70,
                Quantity = 2,
                ImageUrl = "https://images.unsplash.com/photo-1604908176997-125f25cc6f3d"
            }
        });

            // ✅ Save back to session
            HttpContext.Session.SetObject(CART_KEY, cart);
        }

        return View(cart);
    }


    [HttpPost]
    public IActionResult Update(int productId, int qty)
    {
        var cart = HttpContext.Session.GetObject<CartVM>(CART_KEY);

        var item = cart.Items.FirstOrDefault(x => x.ProductId == productId);
        if (item != null)
        {
            item.Quantity = Math.Max(1, qty);
            HttpContext.Session.SetObject(CART_KEY, cart);
        }

        return Ok();
    }
}

//using Microsoft.AspNetCore.Mvc;
//using STHEnterprise.Mvc.Models;

//public class CartController : Controller
//{
//    private const string CART_KEY = "CART";
//    public IActionResult Index()
//    {
//        // ALWAYS return a valid CartVM
//        var cart = HttpContext.Session.GetObject<CartVM>(CART_KEY);

//        if (cart == null)
//        {
//            cart = new CartVM(); // SAFETY
//        }

//        return View(cart);
//    }

//    [HttpPost]
//    public IActionResult Add(int productId, string name, decimal price, string imageUrl)
//    {
//        var cart = HttpContext.Session.GetObject<CartVM>(CART_KEY);

//        var item = cart.Items.FirstOrDefault(x => x.ProductId == productId);

//        if (item == null)
//        {
//            cart.Items.Add(new CartItemVM
//            {
//                ProductId = productId,
//                Name = name,
//                Price = price,
//                Quantity = 1,
//                ImageUrl = imageUrl
//            });
//        }
//        else
//        {
//            item.Quantity++;
//        }

//        HttpContext.Session.SetObject(CART_KEY, cart);
//        return Ok(cart);
//    }

//    [HttpPost]
//    public IActionResult Update(int productId, int qty)
//    {
//        var cart = HttpContext.Session.GetObject<CartVM>(CART_KEY);
//        var item = cart.Items.FirstOrDefault(x => x.ProductId == productId);

//        if (item != null)
//        {
//            if (qty <= 0)
//                cart.Items.Remove(item);
//            else
//                item.Quantity = qty;
//        }

//        HttpContext.Session.SetObject(CART_KEY, cart);
//        return Ok(cart);
//    }

//    [HttpPost]
//    public IActionResult Clear()
//    {
//        HttpContext.Session.Remove(CART_KEY);
//        return Ok();
//    }
//}
