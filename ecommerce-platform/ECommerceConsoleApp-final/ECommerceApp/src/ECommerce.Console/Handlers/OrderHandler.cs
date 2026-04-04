using ECommerce.Application.Cart.Commands;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders.Commands;
using ECommerce.Application.Orders.Queries;
using ECommerce.Console.Services;
using MediatR;

namespace ECommerce.Console.Handlers;

/// <summary>Handles order placement and order viewing.</summary>
public class OrderHandler(IMediator mediator)
{
    public async Task PlaceOrderAsync(Guid customerId, CancellationToken ct)
    {
        // Show cart first
        var cartResult = await mediator.Send(new ViewCartQuery(customerId), ct);
        if (cartResult.IsFailure) { ConsoleDisplayService.Info(cartResult.Error); return; }
        ConsoleDisplayService.PrintCart(cartResult.Value!);

        ConsoleDisplayService.Info("Enter shipping address:");
        ConsoleDisplayService.Prompt("Street");   var street  = ConsoleDisplayService.ReadLine();
        ConsoleDisplayService.Prompt("City");     var city    = ConsoleDisplayService.ReadLine();
        ConsoleDisplayService.Prompt("State");    var state   = ConsoleDisplayService.ReadLine();
        ConsoleDisplayService.Prompt("PIN Code"); var pin     = ConsoleDisplayService.ReadLine();

        if (new[] { street, city, state, pin }.Any(string.IsNullOrWhiteSpace))
        { ConsoleDisplayService.Error("All address fields are required."); return; }

        ConsoleDisplayService.Prompt("Confirm order? (y/n)");
        if (ConsoleDisplayService.ReadLine().ToLower() != "y") return;

        var address = new AddressDto(street, city, state, pin);
        var result  = await mediator.Send(new PlaceOrderCommand(customerId, address), ct);

        if (result.IsFailure) { ConsoleDisplayService.Error(result.Error); return; }

        var order = result.Value!;
        ConsoleDisplayService.Success($"Order placed successfully!");
        ConsoleDisplayService.PrintOrderDetail(order);
        ConsoleDisplayService.Info($"Order Number: {order.OrderNumber}");
        ConsoleDisplayService.Info("Email confirmation sent.");
        ConsoleDisplayService.PressAnyKey();
    }

    public async Task ViewOrdersAsync(Guid customerId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetOrdersQuery(customerId), ct);
        if (result.IsFailure) { ConsoleDisplayService.Error(result.Error); return; }

        ConsoleDisplayService.PrintOrders(result.Value!);

        ConsoleDisplayService.Prompt("Enter order number to view details (or press Enter to skip)");
        var input = ConsoleDisplayService.ReadLine();
        if (!string.IsNullOrWhiteSpace(input) && int.TryParse(input, out var idx)
            && idx >= 1 && idx <= result.Value!.Count)
        {
            ConsoleDisplayService.PrintOrderDetail(result.Value![idx - 1]);
        }
        ConsoleDisplayService.PressAnyKey();
    }

    public async Task CancelOrderAsync(Guid customerId, CancellationToken ct)
    {
        var ordersResult = await mediator.Send(new GetOrdersQuery(customerId), ct);
        if (ordersResult.IsFailure || ordersResult.Value == null || !ordersResult.Value.Any())
        { ConsoleDisplayService.Info("No orders found."); return; }

        var cancellable = ordersResult.Value!
            .Where(o => o.Status is "Pending" or "Confirmed")
            .ToList();

        if (!cancellable.Any())
        { ConsoleDisplayService.Info("No cancellable orders (only Pending/Confirmed orders can be cancelled)."); return; }

        ConsoleDisplayService.PrintOrders(cancellable);
        ConsoleDisplayService.Prompt("Enter order number to cancel");
        if (!int.TryParse(ConsoleDisplayService.ReadLine(), out var idx) || idx < 1 || idx > cancellable.Count)
        { ConsoleDisplayService.Error("Invalid selection."); return; }

        var order = cancellable[idx - 1];
        ConsoleDisplayService.Prompt($"Reason for cancelling {order.OrderNumber}");
        var reason = ConsoleDisplayService.ReadLine();

        var result = await mediator.Send(new CancelOrderCommand(order.Id, customerId, reason), ct);
        if (result.IsFailure) { ConsoleDisplayService.Error(result.Error); return; }

        ConsoleDisplayService.Success($"Order {order.OrderNumber} cancelled. Refund will be processed within 3-5 business days.");
        ConsoleDisplayService.PressAnyKey();
    }
}
