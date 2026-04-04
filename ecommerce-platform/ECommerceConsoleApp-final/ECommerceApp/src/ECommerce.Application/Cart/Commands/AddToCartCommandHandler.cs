using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.ValueObjects;
using MediatR;
using CartEntity = ECommerce.Domain.Entities.Cart;

namespace ECommerce.Application.Cart.Commands;

/// <summary>
/// Handles AddToCartCommand.
///
/// Key design decision: loads Cart WITHOUT its Items collection to avoid EF tracking
/// existing CartItem rows. This prevents spurious UPDATEs on unmodified CartItems during
/// SaveChanges — the root cause of DbUpdateConcurrencyException on repeated adds.
///
/// For quantity merging (same product added twice): loads only the matching CartItem
/// directly rather than the full collection, keeping the tracked set minimal.
/// </summary>
public class AddToCartCommandHandler(
    ICartRepository carts,
    IProductRepository products,
    ICartItemRepository cartItems,
    IUnitOfWork uow) : IRequestHandler<AddToCartCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(AddToCartCommand cmd, CancellationToken ct)
    {
        // Read product for validation — AsNoTracking avoids tracking Product.Price (OwnsOne)
        // which would cause spurious UPDATEs on the Products table during SaveChanges.
        var product = await products.GetByIdReadOnlyAsync(cmd.ProductId, ct);
        if (product is null)
            return Result.Failure<CartDto>($"Product {cmd.ProductId} not found.");
        if (!product.IsActive)
            return Result.Failure<CartDto>("Product is not available.");
        if (product.AvailableQuantity < cmd.Quantity)
            return Result.Failure<CartDto>($"Only {product.AvailableQuantity} units available.");

        // Load cart WITHOUT items — avoids tracking existing CartItems entirely
        var cart = await carts.GetByCustomerIdNoItemsAsync(cmd.CustomerId, ct);
        bool isNew = cart is null;

        if (isNew)
        {
            cart = CartEntity.Create(cmd.CustomerId);
            await carts.AddAsync(cart, ct);
        }

        // Check for existing CartItem for this product — load only the one row we need
        var existing = isNew
            ? null
            : await cartItems.FindByCartAndProductAsync(cart!.Id, cmd.ProductId, ct);

        if (existing is not null)
        {
            // Merge: update only the quantity on the one tracked CartItem
            existing.UpdateQuantity(existing.Quantity + cmd.Quantity);
        }
        else
        {
            // New product in cart: create CartItem and add directly via DbContext
            var newItem = CartItem.Create(cart!.Id, product.Id, product.Name,
                new Money(product.Price.Amount, product.Price.Currency),
                cmd.Quantity);
            await cartItems.AddAsync(newItem, ct);
        }

        // Update cart's UpdatedAt timestamp
        cart!.Touch();

        await uow.SaveChangesAsync(ct);

        // Re-load full cart for the response DTO (in the same scope, tracked)
        var fullCart = await carts.GetByCustomerIdAsync(cmd.CustomerId, ct);
        return Result.Success(ToDto(fullCart!));
    }

    private static CartDto ToDto(CartEntity cart) => new(
        cart.Id,
        cart.CustomerId,
        cart.Items.Select(i => new CartItemDto(
            i.ProductId, i.ProductName,
            i.UnitPrice.Amount, i.Quantity, i.Subtotal.Amount)).ToList(),
        cart.TotalAmount.Amount,
        cart.TotalAmount.Currency
    );
}
