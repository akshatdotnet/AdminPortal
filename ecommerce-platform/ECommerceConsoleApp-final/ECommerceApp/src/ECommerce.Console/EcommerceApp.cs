using ECommerce.Console.Handlers;
using ECommerce.Console.Services;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECommerce.Console;

/// <summary>
/// Interactive menu loop.
/// Each menu selection creates its own child DI scope (fresh DbContext) so there is
/// no EF change-tracker state pollution between successive operations — same pattern
/// ASP.NET Core uses (one scope per HTTP request).
/// </summary>
public class EcommerceApp(
    IServiceScopeFactory scopeFactory,
    ILogger<EcommerceApp> logger)
{
    private Guid   _currentCustomerId;
    private string _currentCustomerName = string.Empty;

    public async Task RunAsync(CancellationToken ct = default)
    {
        await SelectCustomerAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                ConsoleDisplayService.Banner();
                ConsoleDisplayService.MainMenu(_currentCustomerName);

                var choice = ConsoleDisplayService.ReadLine().ToUpperInvariant();

                // Each case creates its own scope → fresh DbContext → no stale tracking
                switch (choice)
                {
                    case "1": await InScope<ProductHandler>((h, c) => h.BrowseAsync(c), ct); break;
                    case "2": await InScope<ProductHandler>((h, c) => h.SearchAsync(c), ct); break;
                    case "3": await InScope<CartHandler>((h, c) => h.AddToCartAsync(_currentCustomerId, c), ct); break;
                    case "4": await InScope<CartHandler>((h, c) => h.RemoveFromCartAsync(_currentCustomerId, c), ct); break;
                    case "5": await InScope<CartHandler>((h, c) => h.ViewCartAsync(_currentCustomerId, c), ct); break;
                    case "6": await InScope<OrderHandler>((h, c) => h.PlaceOrderAsync(_currentCustomerId, c), ct); break;
                    case "7": await InScope<OrderHandler>((h, c) => h.ViewOrdersAsync(_currentCustomerId, c), ct); break;
                    case "8": await InScope<PaymentHandler>((h, c) => h.PayAsync(_currentCustomerId, c), ct); break;
                    case "9": await InScope<OrderHandler>((h, c) => h.CancelOrderAsync(_currentCustomerId, c), ct); break;
                    case "R": await InScope<PaymentHandler>((h, c) => h.RefundAsync(_currentCustomerId, c), ct); break;
                    case "D": await InScope<DemoFlowRunner>((h, c) => h.RunFullDemoAsync(_currentCustomerId, _currentCustomerName, c), ct); break;
                    case "S": await SelectCustomerAsync(ct); break;
                    case "0":
                        ConsoleDisplayService.Success("Thank you for shopping! Goodbye.");
                        return;
                    default:
                        ConsoleDisplayService.Error("Invalid option. Please try again.");
                        await Task.Delay(700, ct);
                        break;
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in menu loop");
                ConsoleDisplayService.Error($"Unexpected error: {ex.Message}");
                ConsoleDisplayService.PressAnyKey();
            }
        }
    }

    /// <summary>
    /// Creates a child DI scope, resolves a handler of type T, invokes the action,
    /// then disposes the scope. This gives every operation its own fresh DbContext.
    /// </summary>
    private async Task InScope<T>(Func<T, CancellationToken, Task> action, CancellationToken ct)
        where T : notnull
    {
        using var scope   = scopeFactory.CreateScope();
        var       handler = scope.ServiceProvider.GetRequiredService<T>();
        await action(handler, ct);
    }

    private async Task SelectCustomerAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var customers   = await db.Customers.ToListAsync(ct);

        if (!customers.Any())
        {
            _currentCustomerId   = Guid.NewGuid();
            _currentCustomerName = "Demo User";
            return;
        }

        var list = customers.Select(c => (c.Id, c.FullName, c.Email.Value)).ToList();

        System.Console.Clear();
        ConsoleDisplayService.Banner();
        System.Console.WriteLine("\n  Welcome! Please select a demo customer:\n");
        for (int i = 0; i < list.Count; i++)
            System.Console.WriteLine($"    [{i + 1}]  {list[i].FullName}  <{list[i].Value}>");

        ConsoleDisplayService.Prompt("\n  Enter number");
        var input = ConsoleDisplayService.ReadLine();

        var idx = int.TryParse(input, out var n) && n >= 1 && n <= list.Count ? n - 1 : 0;
        _currentCustomerId   = list[idx].Id;
        _currentCustomerName = list[idx].FullName;

        ConsoleDisplayService.Success($"Logged in as {_currentCustomerName}");
        await Task.Delay(600, ct);
    }
}
