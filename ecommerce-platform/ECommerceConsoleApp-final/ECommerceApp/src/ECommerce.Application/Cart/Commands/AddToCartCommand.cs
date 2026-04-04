using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Cart.Commands;

public record AddToCartCommand(Guid CustomerId, Guid ProductId, int Quantity)
    : IRequest<Result<CartDto>>;
