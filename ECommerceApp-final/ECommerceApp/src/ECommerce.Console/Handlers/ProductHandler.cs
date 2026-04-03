using ECommerce.Application.Products.Queries;
using ECommerce.Console.Services;
using MediatR;

namespace ECommerce.Console.Handlers;

/// <summary>Handles all product-related console interactions (SRP).</summary>
public class ProductHandler(IMediator mediator)
{
    public async Task BrowseAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new ListProductsQuery(), ct);
        if (result.IsFailure) { ConsoleDisplayService.Error(result.Error); return; }
        ConsoleDisplayService.PrintProducts(result.Value!);
        ConsoleDisplayService.PressAnyKey();
    }

    public async Task SearchAsync(CancellationToken ct)
    {
        ConsoleDisplayService.Prompt("Enter search term");
        var term = ConsoleDisplayService.ReadLine();
        if (string.IsNullOrWhiteSpace(term)) return;

        var result = await mediator.Send(new ListProductsQuery(SearchTerm: term), ct);
        if (result.IsFailure) { ConsoleDisplayService.Error(result.Error); return; }
        ConsoleDisplayService.PrintProducts(result.Value!);
        ConsoleDisplayService.PressAnyKey();
    }
}
