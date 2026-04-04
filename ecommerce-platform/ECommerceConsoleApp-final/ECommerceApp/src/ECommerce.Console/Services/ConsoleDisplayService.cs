using ECommerce.Application.Common.Models;

namespace ECommerce.Console.Services;

/// <summary>
/// Rich console output service.
/// Centralizes all formatting, tables, and banners — SRP: only knows how to display.
/// </summary>
public static class ConsoleDisplayService
{
    // ── Layout constants ──────────────────────────────────
    private const int Width = 70;

    public static void Banner()
    {
        Clear();
        SetColor(ConsoleColor.Cyan);
        PrintLine('═');
        Center("  ███████╗ ██████╗ ██████╗ ███╗   ███╗███╗   ███╗███████╗");
        Center("  ██╔════╝██╔════╝██╔═══██╗████╗ ████║████╗ ████║██╔════╝");
        Center("  █████╗  ██║     ██║   ██║██╔████╔██║██╔████╔██║█████╗  ");
        Center("  ██╔══╝  ██║     ██║   ██║██║╚██╔╝██║██║╚██╔╝██║██╔══╝  ");
        Center("  ███████╗╚██████╗╚██████╔╝██║ ╚═╝ ██║██║ ╚═╝ ██║███████╗");
        Center("  ╚══════╝ ╚═════╝ ╚═════╝ ╚═╝     ╚═╝╚═╝     ╚═╝╚══════╝");
        PrintLine('═');
        Reset();
        Center(".NET 8  ·  Clean Architecture  ·  SOLID Principles");
        PrintLine('─');
        System.Console.WriteLine();
    }

    public static void MainMenu(string customerName)
    {
        SetColor(ConsoleColor.Yellow);
        System.Console.WriteLine($"  👤  Logged in as: {customerName}");
        System.Console.WriteLine();
        Reset();

        MenuHeader("MAIN MENU");
        MenuItem("1", "🛍️  Browse Products");
        MenuItem("2", "🔍  Search Products");
        MenuItem("3", "➕  Add to Cart");
        MenuItem("4", "➖  Remove from Cart");
        MenuItem("5", "🛒  View My Cart");
        MenuItem("6", "📦  Place Order");
        MenuItem("7", "📋  My Orders");
        MenuItem("8", "💳  Pay for Order");
        MenuItem("9", "❌  Cancel Order");
        MenuItem("R", "💸  Request Refund");
        MenuItem("D", "🚀  Run Full Demo Flow (automated)");
        MenuItem("S", "👤  Switch Customer");
        MenuItem("0", "🚪  Exit");
        PrintLine('─');
        Prompt("Select option");
    }

    public static void PrintProducts(IReadOnlyList<ProductDto> products)
    {
        MenuHeader("PRODUCT CATALOGUE");
        if (!products.Any()) { Info("No products found."); return; }

        TableHeader(new[] { "#", "Product", "Price", "Stock", "Category" },
                    new[] { 4, 32, 14, 8, 10 });

        int i = 1;
        foreach (var p in products)
        {
            var stockColor = p.AvailableStock > 10 ? ConsoleColor.Green
                           : p.AvailableStock > 0  ? ConsoleColor.Yellow
                           : ConsoleColor.Red;
            TableRow(new[] { i.ToString(), Truncate(p.Name, 30), $"₹{p.Price:N0}", p.AvailableStock.ToString(), "" },
                     new[] { 4, 32, 14, 8, 10 },
                     stockColor);
            i++;
        }
        PrintLine('─');
    }

    public static void PrintCart(CartDto cart)
    {
        MenuHeader("YOUR CART");
        if (!cart.Items.Any()) { Info("Cart is empty."); return; }

        TableHeader(new[] { "Product", "Unit Price", "Qty", "Subtotal" },
                    new[] { 30, 14, 6, 14 });

        foreach (var item in cart.Items)
            TableRow(new[] { Truncate(item.ProductName, 28), $"₹{item.UnitPrice:N0}", item.Quantity.ToString(), $"₹{item.Subtotal:N0}" },
                     new[] { 30, 14, 6, 14 });

        PrintLine('─');
        SetColor(ConsoleColor.White);
        System.Console.WriteLine($"  {"TOTAL",-48} ₹{cart.TotalAmount:N0}");
        Reset();
        PrintLine('─');
    }

    public static void PrintOrders(IReadOnlyList<OrderDto> orders)
    {
        MenuHeader("YOUR ORDERS");
        if (!orders.Any()) { Info("No orders found."); return; }

        int i = 1;
        foreach (var o in orders)
        {
            var statusColor = o.Status switch
            {
                "Confirmed"  => ConsoleColor.Green,
                "Shipped"    => ConsoleColor.Cyan,
                "Delivered"  => ConsoleColor.Blue,
                "Cancelled"  => ConsoleColor.Red,
                "Refunded"   => ConsoleColor.Magenta,
                _            => ConsoleColor.Yellow
            };

            SetColor(ConsoleColor.Gray);
            System.Console.Write($"  [{i++}] ");
            Reset();
            System.Console.Write($"{o.OrderNumber}  ");
            SetColor(statusColor);
            System.Console.Write($"[{o.Status,-12}]");
            Reset();
            System.Console.WriteLine($"  ₹{o.TotalAmount:N0}   {o.CreatedAt:dd MMM yyyy HH:mm}");
        }
        PrintLine('─');
    }

    public static void PrintOrderDetail(OrderDto o)
    {
        MenuHeader($"ORDER DETAIL — {o.OrderNumber}");
        SetColor(ConsoleColor.Gray);
        System.Console.WriteLine($"  Status  : {o.Status}");
        System.Console.WriteLine($"  Date    : {o.CreatedAt:dd MMM yyyy HH:mm}");
        System.Console.WriteLine($"  Ship To : {o.ShippingAddress.Street}, {o.ShippingAddress.City}");
        if (o.TrackingNumber is not null)
            System.Console.WriteLine($"  Tracking: {o.TrackingNumber}");
        Reset();
        System.Console.WriteLine();

        TableHeader(new[] { "Product", "Unit Price", "Qty", "Subtotal" }, new[] { 30, 14, 6, 14 });
        foreach (var i in o.Items)
            TableRow(new[] { Truncate(i.ProductName, 28), $"₹{i.UnitPrice:N0}", i.Quantity.ToString(), $"₹{i.Subtotal:N0}" },
                     new[] { 30, 14, 6, 14 });
        PrintLine('─');
        System.Console.WriteLine($"  {"TOTAL",-50} ₹{o.TotalAmount:N0}");
        PrintLine('─');
    }

    public static void Success(string message)
    {
        System.Console.WriteLine();
        SetColor(ConsoleColor.Green);
        System.Console.WriteLine($"  ✅  {message}");
        Reset();
        System.Console.WriteLine();
    }

    public static void Error(string message)
    {
        System.Console.WriteLine();
        SetColor(ConsoleColor.Red);
        System.Console.WriteLine($"  ❌  {message}");
        Reset();
        System.Console.WriteLine();
    }

    public static void Info(string message)
    {
        SetColor(ConsoleColor.DarkGray);
        System.Console.WriteLine($"  ℹ  {message}");
        Reset();
    }

    public static void Prompt(string label)
    {
        SetColor(ConsoleColor.White);
        System.Console.Write($"\n  ➤  {label}: ");
        Reset();
    }

    public static string ReadLine() => System.Console.ReadLine()?.Trim() ?? "";
    public static void PressAnyKey() { Info("Press any key to continue..."); System.Console.ReadKey(true); }

    // ── Internals ──
    private static void MenuHeader(string title)
    {
        System.Console.WriteLine();
        SetColor(ConsoleColor.Cyan);
        PrintLine('─');
        System.Console.WriteLine($"  {title}");
        PrintLine('─');
        Reset();
    }

    private static void MenuItem(string key, string label)
    {
        SetColor(ConsoleColor.DarkYellow);
        System.Console.Write($"    [{key}]");
        Reset();
        System.Console.WriteLine($"  {label}");
    }

    private static void TableHeader(string[] cols, int[] widths)
    {
        SetColor(ConsoleColor.DarkGray);
        System.Console.Write("  ");
        for (int i = 0; i < cols.Length; i++)
            System.Console.Write(cols[i].PadRight(widths[i]));
        System.Console.WriteLine();
        PrintLine('·');
        Reset();
    }

    private static void TableRow(string[] cols, int[] widths, ConsoleColor accent = ConsoleColor.Gray)
    {
        System.Console.Write("  ");
        for (int i = 0; i < cols.Length; i++)
        {
            if (i == widths.Length - 2) SetColor(accent);
            System.Console.Write(cols[i].PadRight(widths[i]));
            Reset();
        }
        System.Console.WriteLine();
    }

    private static void PrintLine(char ch) => System.Console.WriteLine(new string(ch, Width));
    private static void Center(string text) => System.Console.WriteLine(text.PadLeft((Width + text.Length) / 2));
    private static void SetColor(ConsoleColor c) => System.Console.ForegroundColor = c;
    private static void Reset() => System.Console.ResetColor();
    private static void Clear() => System.Console.Clear();
    private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max - 1) + "\u2026";
}
