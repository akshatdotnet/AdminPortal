using ECommerce.Application.Common.Models;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Products.Queries;

public class ListProductsQueryHandler(IProductRepository products)
    : IRequestHandler<ListProductsQuery, Result<IReadOnlyList<ProductDto>>>
{
    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(ListProductsQuery query, CancellationToken ct)
    {
        var items = query.SearchTerm is not null
            ? await products.SearchAsync(query.SearchTerm, ct)
            : query.CategoryId.HasValue
                ? await products.GetByCategoryAsync(query.CategoryId.Value, ct)
                : await products.GetAllAsync(ct);

        var dtos = items
            .Where(p => p.IsActive)
            .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price.Amount,
                p.Price.Currency, p.AvailableQuantity, p.CategoryId, p.IsActive))
            .ToList();

        return Result.Success<IReadOnlyList<ProductDto>>(dtos);
    }
}
