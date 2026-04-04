using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Products.Queries;

public record ListProductsQuery(Guid? CategoryId = null, string? SearchTerm = null)
    : IRequest<Result<IReadOnlyList<ProductDto>>>;
