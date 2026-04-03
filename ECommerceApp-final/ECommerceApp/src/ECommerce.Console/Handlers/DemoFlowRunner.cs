using ECommerce.Application.Cart.Commands;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders.Commands;
using ECommerce.Application.Orders.Queries;
using ECommerce.Application.Payment.Commands;
using ECommerce.Application.Products.Queries;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECommerce.Console.Handlers;

/// <summary>
/// Automated end-to-end demo runner.
/// 
/// DESIGN: Each MediatR command is sent through its OWN child DI scope, giving every
/// operation a fresh DbContext. This mirrors how ASP.NET Core handles HTTP requests —
/// one scope per request — and eliminates EF change-tracker state pollution between
/// successive commands in the same console session.
/// </summary>
public class DemoFlowRunner(
    IServiceScopeFactory scopeFactory,
    ILogger<DemoFlowRunner> logger)
{
    private int _passed;
    private int _failed;
    private readonly List<string> _results = [];

    // ── Public entry point ───────────────────────────────────────────
    public async Task RunFullDemoAsync(Guid ignoredId, string ignoredName, CancellationToken ct)
    {
        _passed = 0; _failed = 0; _results.Clear();
        DemoBanner();

        // Always use an isolated demo customer, cleared before each run
        var demoCustomer = await GetOrCreateDemoCustomerAsync(ct);
        await ClearCustomerStateAsync(demoCustomer.Id, ct);

        var customerId = demoCustomer.Id;
        Print(ConsoleColor.White,    $"  Customer    : {demoCustomer.FullName}");
        Print(ConsoleColor.DarkGray, $"  Customer ID : {customerId}");
        Print(ConsoleColor.DarkGray,  "  DbContext   : fresh scope per operation (no stale tracking)");
        System.Console.WriteLine();

        // ── SECTION 1: Product Listing ────────────────────────────────
        await RunSection("SECTION 1 — PRODUCT LISTING", ct, async () =>
        {
            var r = await Send(new ListProductsQuery(), ct);
            Ok(r.IsSuccess, "ListProductsQuery succeeds", r.Error);
            Ok(r.Value?.Count > 0, $"Products seeded — found {r.Value?.Count ?? 0}");
            Info("Count", (r.Value?.Count ?? 0).ToString());

            var search = await Send(new ListProductsQuery(SearchTerm: "iPhone"), ct);
            Ok(search.IsSuccess, "Search 'iPhone' returns results", search.Error);
            Ok(search.Value != null && search.Value.Any(p => p.Name.Contains("iPhone")),
                "Search result contains iPhone product");
            Info("iPhone hits", (search.Value?.Count ?? 0).ToString());

            if (r.Value?.Count > 0)
            {
                var detail = await Send(new GetProductByIdQuery(r.Value[0].Id), ct);
                Ok(detail.IsSuccess, $"GetProductByIdQuery({r.Value[0].Name})", detail.Error);
            }
        });

        // ── SECTION 2: Cart ───────────────────────────────────────────
        ProductDto? productA = null;
        ProductDto? productB = null;

        await RunSection("SECTION 2 — ADD / REMOVE / VIEW CART", ct, async () =>
        {
            var list = (await Send(new ListProductsQuery(), ct)).Value;
            if (list == null || list.Count < 2) { Ok(false, "Need >= 2 products"); return; }

            productA = list.First(p => p.AvailableStock >= 3);
            productB = list.First(p => p.Id != productA.Id && p.AvailableStock >= 1);

            // Each Send() call gets its own fresh DbContext — no stale tracking
            var a1 = await Send(new AddToCartCommand(customerId, productA.Id, 2), ct);
            Ok(a1.IsSuccess, $"Add '{productA.Name}' x2 to cart", a1.Error);
            Info("Cart total after first add", $"Rs.{a1.Value?.TotalAmount:N0}");

            var a2 = await Send(new AddToCartCommand(customerId, productB.Id, 1), ct);
            Ok(a2.IsSuccess, $"Add '{productB.Name}' x1 to cart", a2.Error);
            Info("Cart total after second add", $"Rs.{a2.Value?.TotalAmount:N0}");

            var view = await Send(new ViewCartQuery(customerId), ct);
            Ok(view.IsSuccess, "ViewCartQuery succeeds", view.Error);
            Ok(view.Value?.Items.Count == 2, "Cart has 2 distinct items",
                $"got {view.Value?.Items.Count}");

            // Add productA again → quantities should merge
            var a3 = await Send(new AddToCartCommand(customerId, productA.Id, 1), ct);
            Ok(a3.IsSuccess, $"Add '{productA.Name}' x1 again (merge)", a3.Error);
            var merged = a3.Value?.Items.FirstOrDefault(i => i.ProductId == productA.Id);
            Ok(merged?.Quantity == 3, "Merged quantity = 3", $"got {merged?.Quantity}");

            // Remove productB
            var rem = await Send(new RemoveFromCartCommand(customerId, productB.Id), ct);
            Ok(rem.IsSuccess, $"Remove '{productB.Name}' from cart", rem.Error);
            Ok(rem.Value?.Items.Count == 1, "Cart has 1 item after removal",
                $"got {rem.Value?.Items.Count}");

            // Re-add productB so order placement has 2 items
            var a4 = await Send(new AddToCartCommand(customerId, productB.Id, 1), ct);
            Ok(a4.IsSuccess, $"Re-add '{productB.Name}' for order", a4.Error);
            Info("Final cart total", $"Rs.{a4.Value?.TotalAmount:N0}");
            Info("Final cart items", (a4.Value?.Items.Count ?? 0).ToString());
        });

        // ── SECTION 3: Place Order ────────────────────────────────────
        OrderDto? order1 = null;

        await RunSection("SECTION 3 — PLACE ORDER", ct, async () =>
        {
            var r = await Send(new PlaceOrderCommand(customerId,
                new AddressDto("42 MG Road", "Mumbai", "Maharashtra", "400001")), ct);
            Ok(r.IsSuccess, "PlaceOrderCommand succeeds", r.Error);
            Ok(r.Value?.Status == "Pending", $"Order status = Pending (got '{r.Value?.Status}')");
            Ok((r.Value?.Items.Count ?? 0) > 0, "Order has items");
            order1 = r.Value;
            Info("Order number", order1?.OrderNumber ?? "-");
            Info("Order total",  $"Rs.{order1?.TotalAmount:N0}");

            // Cart must be empty after order
            var cart = await Send(new ViewCartQuery(customerId), ct);
            Ok(cart.IsFailure, "Cart empty after order is placed");

            // Order visible in customer history
            var orders = await Send(new GetOrdersQuery(customerId), ct);
            Ok(orders.IsSuccess && order1 != null &&
               orders.Value!.Any(o => o.OrderNumber == order1.OrderNumber),
               "New order appears in customer order list");
        });

        // ── SECTION 4: Payment ────────────────────────────────────────
        await RunSection("SECTION 4 — PAYMENT (COD — always succeeds)", ct, async () =>
        {
            if (order1 == null) { Ok(false, "SKIPPED — no order from section 3"); return; }

            var r = await Send(
                new ProcessPaymentCommand(order1.Id, customerId, PaymentMethod.CashOnDelivery), ct);
            Ok(r.IsSuccess, "ProcessPaymentCommand (COD) succeeds", r.Error);
            Ok(r.Value?.Status == "Captured",
                $"Payment status = Captured (got '{r.Value?.Status}')");
            Info("Transaction ID", r.Value?.GatewayTransactionId ?? "-");
            Info("Amount paid",    $"Rs.{r.Value?.Amount:N0}");

            // Order status must update to Confirmed
            var orders = await Send(new GetOrdersQuery(customerId), ct);
            var paid   = orders.Value?.FirstOrDefault(o => o.Id == order1.Id);
            Ok(paid?.Status == "Confirmed",
                $"Order status = Confirmed after payment (got '{paid?.Status}')");

            // Duplicate payment must be rejected
            var dup = await Send(
                new ProcessPaymentCommand(order1.Id, customerId, PaymentMethod.CashOnDelivery), ct);
            Ok(dup.IsFailure, "Duplicate payment correctly rejected (idempotency guard)");
        });

        // ── SECTION 5: Cancel + Refund ────────────────────────────────
        await RunSection("SECTION 5 — CANCEL AND REFUND", ct, async () =>
        {
            if (productA == null) { Ok(false, "SKIPPED — productA not loaded"); return; }

            // Fresh order to cancel
            var addR = await Send(new AddToCartCommand(customerId, productA.Id, 1), ct);
            Ok(addR.IsSuccess, "Add item for cancel-test order", addR.Error);

            var place2 = await Send(new PlaceOrderCommand(customerId,
                new AddressDto("10 Park St", "Pune", "Maharashtra", "411001")), ct);
            Ok(place2.IsSuccess, "Place second order for cancel test", place2.Error);
            var order2 = place2.Value!;
            Info("Cancel-test order", order2.OrderNumber);

            var cancel2 = await Send(
                new CancelOrderCommand(order2.Id, customerId, "Demo cancel flow test"), ct);
            Ok(cancel2.IsSuccess, "CancelOrderCommand succeeds", cancel2.Error);
            Ok(cancel2.Value?.Status == "Cancelled",
                $"Order status = Cancelled (got '{cancel2.Value?.Status}')");

            // Double-cancel must fail
            var dblCancel = await Send(
                new CancelOrderCommand(order2.Id, customerId, "double cancel"), ct);
            Ok(dblCancel.IsFailure, "Second cancel rejected by domain rule");

            // Refund without payment must fail gracefully
            var noRefund = await Send(new RefundPaymentCommand(order2.Id, customerId), ct);
            Ok(noRefund.IsFailure, "Refund without prior payment fails gracefully");
            Info("No-payment refund error", noRefund.Error);

            // Full cycle: add → place → pay (COD) → cancel → refund
            if (productB != null)
            {
                var addB = await Send(new AddToCartCommand(customerId, productB.Id, 1), ct);
                Ok(addB.IsSuccess, "Add item for pay+cancel+refund cycle", addB.Error);

                var place3 = await Send(new PlaceOrderCommand(customerId,
                    new AddressDto("5 Ring Rd", "Delhi", "Delhi", "110001")), ct);
                Ok(place3.IsSuccess, "Place third order (for full cycle)", place3.Error);
                var order3 = place3.Value!;

                var pay3 = await Send(
                    new ProcessPaymentCommand(order3.Id, customerId, PaymentMethod.CashOnDelivery), ct);
                Ok(pay3.IsSuccess, "Pay third order (COD)", pay3.Error);

                var cancel3 = await Send(
                    new CancelOrderCommand(order3.Id, customerId, "Paid then cancelled"), ct);
                Ok(cancel3.IsSuccess, "Cancel paid+confirmed order", cancel3.Error);

                var refund3 = await Send(new RefundPaymentCommand(order3.Id, customerId), ct);
                Ok(refund3.IsSuccess, "RefundPaymentCommand succeeds", refund3.Error);
                Ok(refund3.Value?.Status == "Refunded",
                    $"Payment status = Refunded (got '{refund3.Value?.Status}')");
                Info("Final refund status", refund3.Value?.Status ?? "-");
            }
        });

        // ── SECTION 6: Domain Rules & Edge Cases ──────────────────────
        await RunSection("SECTION 6 — DOMAIN RULES AND EDGE CASES", ct, async () =>
        {
            // Non-existent product
            var bad1 = await Send(new AddToCartCommand(customerId, Guid.NewGuid(), 1), ct);
            Ok(bad1.IsFailure, "Add non-existent product → fails");

            // Excessive quantity
            if (productA != null)
            {
                var bad2 = await Send(new AddToCartCommand(customerId, productA.Id, 999_999), ct);
                Ok(bad2.IsFailure, "Add quantity beyond available stock → fails");
            }

            // Order with empty cart
            var bad3 = await Send(new PlaceOrderCommand(customerId,
                new AddressDto("X", "Y", "Z", "000000")), ct);
            Ok(bad3.IsFailure, "Place order with empty cart → fails");

            // Wrong customer cannot cancel another's order
            var allOrders = (await Send(new GetOrdersQuery(customerId), ct)).Value;
            if (allOrders?.Count > 0)
            {
                var bad4 = await Send(
                    new CancelOrderCommand(allOrders[0].Id, Guid.NewGuid(), "auth test"), ct);
                Ok(bad4.IsFailure, "Wrong customer cannot cancel another's order");
            }

            // Pay already-confirmed order
            var confirmed = allOrders?.FirstOrDefault(o => o.Status == "Confirmed");
            if (confirmed != null)
            {
                var bad5 = await Send(
                    new ProcessPaymentCommand(confirmed.Id, customerId, PaymentMethod.CashOnDelivery), ct);
                Ok(bad5.IsFailure, "Paying already-confirmed order → rejected");
            }

            // Cancel non-existent order
            var bad6 = await Send(
                new CancelOrderCommand(Guid.NewGuid(), customerId, "ghost order"), ct);
            Ok(bad6.IsFailure, "Cancel non-existent order → fails");
        });

        Summary();
    }

    // ── Core helper: send command in its own fresh child scope ────────────────
    // This is the critical architectural fix. Each call creates:
    //   new DI scope → new DbContext → fresh change tracker → no cross-command contamination
    private async Task<Result<T>> Send<T>(IRequest<Result<T>> request, CancellationToken ct)
    {
        using var childScope = scopeFactory.CreateScope();
        var mediator = childScope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(request, ct);
    }

    // ── State isolation ───────────────────────────────────────────────────────
    private async Task<Domain.Entities.Customer> GetOrCreateDemoCustomerAsync(CancellationToken ct)
    {
        const string demoEmail = "demo.runner@ecommerce.test";
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existing = await db.Customers
            .FirstOrDefaultAsync(c => c.Email.Value == demoEmail, ct);
        if (existing != null) return existing;

        var customer = Domain.Entities.Customer.Create("Demo", "Runner", demoEmail, "+91-0000000000");
        await db.Customers.AddAsync(customer, ct);
        await db.SaveChangesAsync(ct);
        return customer;
    }

    private async Task ClearCustomerStateAsync(Guid customerId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cart = await db.Carts.FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
        if (cart != null)
        {
            // Cart.Items navigation is ignored by EF config — delete CartItems explicitly
            var items = await db.CartItems.Where(i => i.CartId == cart.Id).ToListAsync(ct);
            db.CartItems.RemoveRange(items);
            db.Carts.Remove(cart);
            await db.SaveChangesAsync(ct);
        }
    }

    // ── Section runner ────────────────────────────────────────────────────────
    private async Task RunSection(string title, CancellationToken ct, Func<Task> body)
    {
        System.Console.WriteLine();
        Print(ConsoleColor.Cyan, new string('=', 68));
        Print(ConsoleColor.Cyan, "  " + title);
        Print(ConsoleColor.Cyan, new string('=', 68));
        System.Console.WriteLine();

        try { await body(); }
        catch (Exception ex)
        {
            Fail("UNEXPECTED EXCEPTION: " + ex.Message);
            System.Console.WriteLine();
            logger.LogError(ex, "Demo section threw: {Title}", title);
        }
    }

    // ── Assert helpers ────────────────────────────────────────────────────────
    private void Ok(bool condition, string label, string detail = "")
    {
        if (condition) Pass(label);
        else           Fail(label + (string.IsNullOrEmpty(detail) ? "" : "  [" + detail + "]"));
        System.Console.WriteLine();
    }

    private void Pass(string label)
    {
        _passed++;
        _results.Add("PASS: " + label);
        System.Console.ForegroundColor = ConsoleColor.Green;
        System.Console.Write("    [PASS]  ");
        System.Console.ResetColor();
        System.Console.Write(label);
    }

    private void Fail(string label)
    {
        _failed++;
        _results.Add("FAIL: " + label);
        System.Console.ForegroundColor = ConsoleColor.Red;
        System.Console.Write("    [FAIL]  ");
        System.Console.ResetColor();
        System.Console.Write(label);
    }

    private static void Info(string key, string value)
    {
        System.Console.ForegroundColor = ConsoleColor.DarkGray;
        System.Console.Write("    [INFO]  " + key + ": ");
        System.Console.ForegroundColor = ConsoleColor.White;
        System.Console.WriteLine(value);
        System.Console.ResetColor();
    }

    private static void Print(ConsoleColor color, string text)
    {
        System.Console.ForegroundColor = color;
        System.Console.WriteLine(text);
        System.Console.ResetColor();
    }

    private void Summary()
    {
        System.Console.WriteLine();
        Print(ConsoleColor.Cyan, new string('=', 68));
        Print(ConsoleColor.Cyan, "  SUMMARY");
        Print(ConsoleColor.Cyan, new string('=', 68));
        System.Console.WriteLine();
        foreach (var r in _results)
        {
            System.Console.ForegroundColor = r.StartsWith("PASS") ? ConsoleColor.Green : ConsoleColor.Red;
            System.Console.WriteLine("  " + r);
        }
        System.Console.ResetColor();
        System.Console.WriteLine();
        System.Console.ForegroundColor = ConsoleColor.Green;
        System.Console.Write($"  Passed : {_passed}   ");
        System.Console.ForegroundColor = _failed > 0 ? ConsoleColor.Red : ConsoleColor.Green;
        System.Console.Write($"Failed : {_failed}   ");
        System.Console.ForegroundColor = ConsoleColor.White;
        System.Console.WriteLine($"Total : {_passed + _failed}");
        System.Console.ResetColor();
        System.Console.WriteLine();
        if (_failed == 0)
            Print(ConsoleColor.Green, "  ALL CHECKS PASSED — complete e-commerce flow is healthy!");
        else
            Print(ConsoleColor.Yellow, $"  {_failed} check(s) failed — review output above.");
        Print(ConsoleColor.Cyan, new string('=', 68));
        System.Console.WriteLine();
        System.Console.Write("  Press any key to return to menu...");
        System.Console.ReadKey(true);
    }

    private static void DemoBanner()
    {
        System.Console.Clear();
        System.Console.ForegroundColor = ConsoleColor.Magenta;
        System.Console.WriteLine();
        System.Console.WriteLine("  ================================================================");
        System.Console.WriteLine("   E-COMMERCE AUTOMATED DEMO FLOW  |  .NET 8  |  Clean Arch");
        System.Console.WriteLine("  ================================================================");
        System.Console.ResetColor();
        System.Console.ForegroundColor = ConsoleColor.DarkGray;
        System.Console.WriteLine("  Sections: Listing | Cart | Order | Payment | Cancel+Refund | Edge Cases");
        System.Console.ForegroundColor = ConsoleColor.DarkYellow;
        System.Console.WriteLine("  No input required. Each operation uses an isolated DbContext scope.");
        System.Console.ResetColor();
        System.Console.WriteLine();
    }
}
