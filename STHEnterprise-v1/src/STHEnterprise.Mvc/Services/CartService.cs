namespace STHEnterprise.Mvc.Services
{
    public class CartService
    {
        private const string CART_KEY = "CART";

        public static List<CartItem> GetCart(HttpContext context)
        {
            return context.Session.GetObject<List<CartItem>>(CART_KEY) ?? new();
        }

        public static void SaveCart(HttpContext context, List<CartItem> cart)
        {
            context.Session.SetObject(CART_KEY, cart);
        }

        public static void AddToCart(HttpContext context, CartItem item)
        {
            var cart = GetCart(context);
            var existing = cart.FirstOrDefault(x => x.ProductId == item.ProductId);

            if (existing != null)
                existing.Qty++;
            else
                cart.Add(item);

            SaveCart(context, cart);
        }

        public static void Remove(HttpContext context, int productId)
        {
            var cart = GetCart(context);
            cart.RemoveAll(x => x.ProductId == productId);
            SaveCart(context, cart);
        }

        public static void Clear(HttpContext context)
        {
            context.Session.Remove(CART_KEY);
        }
    }

}
