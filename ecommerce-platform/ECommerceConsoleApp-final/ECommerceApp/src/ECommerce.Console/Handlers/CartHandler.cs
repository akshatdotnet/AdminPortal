using ECommerce.Application.Cart.Commands;
using ECommerce.Application.Products.Queries;
using ECommerce.Console.Services;
using MediatR;

namespace ECommerce.Console.Handlers;

/// <summary>Handles cart console interactions (SRP — only cart UI logic).</summary>
public class CartHandler(IMediator mediator)
{
    public async Task ViewCartAsync(Guid customerId, CancellationToken ct)
    {
        var result = await mediator.Send(new ViewCartQuery(customerId), ct);
        if (result.IsFailure) { ConsoleDisplayService.Info(result.Error); return; }
        ConsoleDisplayService.PrintCart(result.Value!);
        ConsoleDisplayService.PressAnyKey();
    }

    public async Task AddToCartAsync(Guid customerId, CancellationToken ct)
    {
        var productsResult = await mediator.Send(new ListProductsQuery(), ct);
        if (productsResult.IsFailure || productsResult.Value == null)
        { ConsoleDisplayService.Error("Could not load products."); return; }

        var productList = productsResult.Value;
        ConsoleDisplayService.PrintProducts(productList);

        ConsoleDisplayService.Prompt("Enter product number to add");
        if (!int.TryParse(ConsoleDisplayService.ReadLine(), out var idx) || idx < 1 || idx > productList.Count)
        { ConsoleDisplayService.Error("Invalid selection."); return; }

        var product = productList[idx - 1];

        ConsoleDisplayService.Prompt($"Quantity (available: {product.AvailableStock})");
        if (!int.TryParse(ConsoleDisplayService.ReadLine(), out var qty) || qty < 1)
        { ConsoleDisplayService.Error("Invalid quantity."); return; }

        var result = await mediator.Send(new AddToCartCommand(customerId, product.Id, qty), ct);
        if (result.IsFailure) { ConsoleDisplayService.Error(result.Error); return; }

        ConsoleDisplayService.Success($"Added {qty}x '{product.Name}' to cart.");
        ConsoleDisplayService.PrintCart(result.Value!);
        ConsoleDisplayService.PressAnyKey();
    }

    public async Task RemoveFromCartAsync(Guid customerId, CancellationToken ct)
    {
        var cartResult = await mediator.Send(new ViewCartQuery(customerId), ct);
        if (cartResult.IsFailure) { ConsoleDisplayService.Info(cartResult.Error); return; }

        var cart = cartResult.Value!;
        ConsoleDisplayService.PrintCart(cart);

        ConsoleDisplayService.Prompt("Enter item number to remove");
        if (!int.TryParse(ConsoleDisplayService.ReadLine(), out var idx) || idx < 1 || idx > cart.Items.Count)
        { ConsoleDisplayService.Error("Invalid selection."); return; }

        var item = cart.Items[idx - 1];
        var result = await mediator.Send(new RemoveFromCartCommand(customerId, item.ProductId), ct);
        if (result.IsFailure) { ConsoleDisplayService.Error(result.Error); return; }

        ConsoleDisplayService.Success($"Removed '{item.ProductName}' from cart.");
        ConsoleDisplayService.PressAnyKey();
    }
}
