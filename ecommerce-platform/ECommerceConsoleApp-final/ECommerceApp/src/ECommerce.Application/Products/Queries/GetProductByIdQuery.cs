using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Products.Queries;

public record GetProductByIdQuery(Guid ProductId) : IRequest<Result<ProductDto>>;
