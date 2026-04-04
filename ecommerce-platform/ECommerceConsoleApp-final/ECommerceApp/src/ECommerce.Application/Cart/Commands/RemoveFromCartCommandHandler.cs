using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Cart.Commands;

public class RemoveFromCartCommandHandler(
    ICartRepository carts,
    ICartItemRepository cartItems,
    IUnitOfWork uow) : IRequestHandler<RemoveFromCartCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(RemoveFromCartCommand cmd, CancellationToken ct)
    {
        var cart = await carts.GetByCustomerIdAsync(cmd.CustomerId, ct);
        if (cart is null)
            return Result.Failure<CartDto>("Cart not found.");

        var item = cart.Items.FirstOrDefault(i => i.ProductId == cmd.ProductId);
        if (item is null)
            return Result.Failure<CartDto>("Item not found in cart.");

        // Explicitly remove the CartItem row (EF no longer manages via navigation cascade)
        cartItems.Remove(item);
        cart.Touch();

        await uow.SaveChangesAsync(ct);

        // Reload to get accurate remaining items
        var updated = await carts.GetByCustomerIdAsync(cmd.CustomerId, ct);
        var result = updated ?? cart;

        return Result.Success(new CartDto(
            result.Id, result.CustomerId,
            result.Items.Select(i => new CartItemDto(
                i.ProductId, i.ProductName, i.UnitPrice.Amount, i.Quantity, i.Subtotal.Amount)).ToList(),
            result.TotalAmount.Amount, result.TotalAmount.Currency));
    }
}
