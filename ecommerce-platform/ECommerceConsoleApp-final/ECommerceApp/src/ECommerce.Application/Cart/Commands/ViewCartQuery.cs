using ECommerce.Application.Common.Models;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Cart.Commands;

public record ViewCartQuery(Guid CustomerId) : IRequest<Result<CartDto>>;

public class ViewCartQueryHandler(ICartRepository carts) : IRequestHandler<ViewCartQuery, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(ViewCartQuery query, CancellationToken ct)
    {
        var cart = await carts.GetByCustomerIdAsync(query.CustomerId, ct);
        if (cart is null || !cart.Items.Any())
            return Result.Failure<CartDto>("Your cart is empty.");

        return Result.Success(new CartDto(
            cart.Id, cart.CustomerId,
            cart.Items.Select(i => new CartItemDto(i.ProductId, i.ProductName, i.UnitPrice.Amount, i.Quantity, i.Subtotal.Amount)).ToList(),
            cart.TotalAmount.Amount, cart.TotalAmount.Currency));
    }
}
