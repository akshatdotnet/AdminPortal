using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Cart.Commands;

public record RemoveFromCartCommand(Guid CustomerId, Guid ProductId) : IRequest<Result<CartDto>>;
