using ECommerce.Application.Common.Models;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Products.Queries;

public class GetProductByIdQueryHandler(IProductRepository products)
    : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(GetProductByIdQuery query, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(query.ProductId, ct);
        if (product is null)
            return Result.Failure<ProductDto>($"Product {query.ProductId} not found.");

        return Result.Success(new ProductDto(product.Id, product.Name, product.Description,
            product.Price.Amount, product.Price.Currency,
            product.AvailableQuantity, product.CategoryId, product.IsActive));
    }
}
