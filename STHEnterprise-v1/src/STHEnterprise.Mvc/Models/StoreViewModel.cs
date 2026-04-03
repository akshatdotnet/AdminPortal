namespace STHEnterprise.Mvc.Models;

public class StoreViewModel
{
    public StoreInfoVM StoreInfo { get; set; } = new();
    public List<CategoryVM> Categories { get; set; } = new();
    public List<ProductVM> Products { get; set; } = new();

    //public StoreInfoVM StoreInfo { get; set; } = new();

    //    public List<CategoryVM> Categories { get; set; } = new();

    //    public List<ProductVM> Products { get; set; } = new();
}

/* ============================= */

public class CategoryVM
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool Active { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;

   

    // public int Id { get; set; }

    //public string Name { get; set; } = string.Empty;

    //public int Count { get; set; }

    //public bool Active { get; set; }
}

/* ============================= */

public class ProductVM
{
    public int Id { get; set; }
    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal OriginalPrice { get; set; }

    public string Image { get; set; } = string.Empty;
    public int DiscountPercent =>
        OriginalPrice > 0 ? (int)((OriginalPrice - Price) / OriginalPrice * 100) : 0;


}

/* ============================= */

public class StoreInfoVM
{
    public string Name { get; set; } = "Suraj Tea House";
    public string StoreName { get; set; } = "Suraj Tea House";
    
    public string Email { get; set; } = "surajteahouse@gmail.com";
    public string Address { get; set; } =
        "A B Holkar Marg, Surya Nagar, Vikhroli West, Mumbai 400083";
    public string Description { get; set; } = "Fresh groceries & services";

}







//using STHEnterprise.Mvc.Models;


//public class StoreViewModel
//{
//    public StoreInfoVM StoreInfo { get; set; } = new();

//    public List<CategoryVM> Categories { get; set; } = new();

//    public List<ProductVM> Products { get; set; } = new();
//}

//public class CategoryVM
//{
//    public int Id { get; set; }
//    public string Name { get; set; } = string.Empty;
//    public int Count { get; set; }
//    // Used by UI (fixes CS1061 Active error)
//    public bool Active { get; set; }
//}

//public class ProductVM
//{
//    public int Id { get; set; }
//    public string Name { get; set; } = string.Empty;
//    public decimal Price { get; set; }
//    public decimal OriginalPrice { get; set; }
//    public string Image { get; set; } = string.Empty;
//    public int CategoryId { get; set; }
//}

//public class StoreInfoVM
//{
//    public string StoreName { get; set; } = string.Empty;
//    public string Description { get; set; } = string.Empty;

//    public string Name { get; set; }
//    public string Email { get; set; }
//    public string Address { get; set; }
//}






//public class StoreViewModel
//{
//    public List<CategoryVm> Categories { get; set; }
//    public List<ProductVm> Products { get; set; }
//    public StoreInfoVm StoreInfo { get; set; }
//    public string StoreName { get; internal set; }
//}

//public record CategoryVm(string Name, int Count, bool Active = false);

//public record ProductVm(int Id, string Name, string Image, decimal Price);

//public class StoreInfoVm
//{
//    public string Name { get; set; }
//    public string Email { get; set; }
//    public string Address { get; set; }
//}
