using Cart.Application.DTOs;
using Cart.Application.Interfaces;
using Common.Domain.Primitives;
using FluentValidation;
using MediatR;

namespace Cart.Application.Commands;

// GET
public sealed record GetCartQuery(Guid CustomerId) : IRequest<Result<CartDto>>;
public sealed class GetCartHandler(ICartRepository repo)
    : IRequestHandler<GetCartQuery, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(GetCartQuery q, CancellationToken ct)
    {
        var c = await repo.GetAsync(q.CustomerId, ct);
        return c is null
            ? Result.Failure<CartDto>(Error.NotFound("Cart", q.CustomerId))
            : Result.Success(CartDto.FromDomain(c));
    }
}

// ADD ITEM
public sealed record AddToCartCommand(
    Guid CustomerId, Guid ProductId, string ProductName,
    string Sku, decimal UnitPrice, int Quantity, string? ImageUrl)
    : IRequest<Result<CartDto>>;

public sealed class AddToCartValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).InclusiveBetween(1, 100);
        RuleFor(x => x.UnitPrice).GreaterThan(0);
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(200);
    }
}

public sealed class AddToCartHandler(ICartRepository repo)
    : IRequestHandler<AddToCartCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(AddToCartCommand cmd, CancellationToken ct)
    {
        var cart = await repo.GetOrCreateAsync(cmd.CustomerId, ct);
        cart.AddItem(cmd.ProductId, cmd.ProductName, cmd.Sku, cmd.UnitPrice, cmd.Quantity, cmd.ImageUrl);
        await repo.SaveAsync(cart, ct);
        return Result.Success(CartDto.FromDomain(cart));
    }
}

// UPDATE QUANTITY
public sealed record UpdateCartItemCommand(Guid CustomerId, Guid ProductId, int NewQty)
    : IRequest<Result<CartDto>>;
public sealed class UpdateCartItemHandler(ICartRepository repo)
    : IRequestHandler<UpdateCartItemCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(UpdateCartItemCommand cmd, CancellationToken ct)
    {
        var c = await repo.GetAsync(cmd.CustomerId, ct);
        if (c is null) return Result.Failure<CartDto>(Error.NotFound("Cart", cmd.CustomerId));
        try { c.UpdateItemQuantity(cmd.ProductId, cmd.NewQty); }
        catch (InvalidOperationException ex)
        { return Result.Failure<CartDto>(Error.NotFound("CartItem", ex.Message)); }
        await repo.SaveAsync(c, ct);
        return Result.Success(CartDto.FromDomain(c));
    }
}

// REMOVE ITEM
public sealed record RemoveCartItemCommand(Guid CustomerId, Guid ProductId)
    : IRequest<Result<CartDto>>;
public sealed class RemoveCartItemHandler(ICartRepository repo)
    : IRequestHandler<RemoveCartItemCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(RemoveCartItemCommand cmd, CancellationToken ct)
    {
        var c = await repo.GetAsync(cmd.CustomerId, ct);
        if (c is null) return Result.Failure<CartDto>(Error.NotFound("Cart", cmd.CustomerId));
        c.RemoveItem(cmd.ProductId);
        await repo.SaveAsync(c, ct);
        return Result.Success(CartDto.FromDomain(c));
    }
}

// APPLY COUPON
public sealed record ApplyCouponCommand(Guid CustomerId, string CouponCode)
    : IRequest<Result<CartDto>>;
public sealed class ApplyCouponHandler(ICartRepository cartRepo, ICouponServiceClient couponClient)
    : IRequestHandler<ApplyCouponCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(ApplyCouponCommand cmd, CancellationToken ct)
    {
        var c = await cartRepo.GetAsync(cmd.CustomerId, ct);
        if (c is null) return Result.Failure<CartDto>(Error.NotFound("Cart", cmd.CustomerId));
        var cr = await couponClient.ValidateAsync(cmd.CouponCode, c.Subtotal, ct);
        if (!cr.IsSuccess) return Result.Failure<CartDto>(cr.Error);
        c.ApplyCoupon(cmd.CouponCode, cr.Value);
        await cartRepo.SaveAsync(c, ct);
        return Result.Success(CartDto.FromDomain(c));
    }
}

// REMOVE COUPON
public sealed record RemoveCouponCommand(Guid CustomerId) : IRequest<Result<CartDto>>;
public sealed class RemoveCouponHandler(ICartRepository repo)
    : IRequestHandler<RemoveCouponCommand, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(RemoveCouponCommand cmd, CancellationToken ct)
    {
        var c = await repo.GetAsync(cmd.CustomerId, ct);
        if (c is null) return Result.Failure<CartDto>(Error.NotFound("Cart", cmd.CustomerId));
        c.RemoveCoupon();
        await repo.SaveAsync(c, ct);
        return Result.Success(CartDto.FromDomain(c));
    }
}

// CLEAR
public sealed record ClearCartCommand(Guid CustomerId) : IRequest<Result>;
public sealed class ClearCartHandler(ICartRepository repo)
    : IRequestHandler<ClearCartCommand, Result>
{
    public async Task<Result> Handle(ClearCartCommand cmd, CancellationToken ct)
    {
        await repo.DeleteAsync(cmd.CustomerId, ct);
        return Result.Success();
    }
}
