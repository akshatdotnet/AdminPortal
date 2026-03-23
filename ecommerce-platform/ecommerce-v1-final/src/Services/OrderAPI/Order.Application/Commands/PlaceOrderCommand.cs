using Common.Domain.Primitives;
using FluentValidation;
using MediatR;
using Order.Application.DTOs;
using Order.Application.Interfaces;
using Order.Domain.Entities;

using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Application.Commands;

public sealed record PlaceOrderCommand(
    Guid CustomerId,
    string ShippingFullName, string ShippingStreet, string ShippingCity,
    string ShippingState, string ShippingPostalCode, string ShippingCountry, string ShippingPhone,
    IReadOnlyList<OrderItemRequest> Items,
    string? CouponCode, string? Notes) : IRequest<Result<PlaceOrderResponse>>;

public sealed record OrderItemRequest(
    Guid ProductId, string ProductName, string Sku, decimal UnitPrice, int Quantity);

public sealed class PlaceOrderValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ShippingFullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ShippingStreet).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ShippingCity).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ShippingPostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.ShippingCountry).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ShippingPhone).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must have at least one item.");
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(x => x.ProductId).NotEmpty();
            i.RuleFor(x => x.Quantity).InclusiveBetween(1, 100);
            i.RuleFor(x => x.UnitPrice).GreaterThan(0);
        });
    }
}

public sealed class PlaceOrderCommandHandler(
    IOrderRepository orderRepo,
    IUnitOfWorkOrder uow,
    IProductServiceClient productClient,
    ICouponServiceClient couponClient)
    : IRequestHandler<PlaceOrderCommand, Result<PlaceOrderResponse>>
{
    public async Task<Result<PlaceOrderResponse>> Handle(
        PlaceOrderCommand cmd, CancellationToken ct)
    {
        // 1. Reserve stock for each item
        foreach (var item in cmd.Items)
        {
            var sr = await productClient.ReserveStockAsync(item.ProductId, item.Quantity, ct);
            if (!sr.IsSuccess) return Result.Failure<PlaceOrderResponse>(sr.Error);
        }

        // 2. Build the order aggregate
        var address = new ShippingAddress
        {
            FullName   = cmd.ShippingFullName,  Street    = cmd.ShippingStreet,
            City       = cmd.ShippingCity,       State     = cmd.ShippingState,
            PostalCode = cmd.ShippingPostalCode, Country   = cmd.ShippingCountry,
            Phone      = cmd.ShippingPhone
        };
        var order = OrderEntity.Create(cmd.CustomerId, address, cmd.Notes);
        foreach (var item in cmd.Items)
            order.AddItem(item.ProductId, item.ProductName, item.Sku, item.UnitPrice, item.Quantity);

        // 3. Apply coupon if provided
        if (!string.IsNullOrEmpty(cmd.CouponCode))
        {
            var cr = await couponClient.ValidateAsync(cmd.CouponCode, order.Subtotal, ct);
            if (cr.IsSuccess) order.ApplyCoupon(cmd.CouponCode, cr.Value);
        }

        // 4. Set shipping ($9.99 flat) and tax (8%)
        order.SetShipping(9.99m);
        order.SetTax(Math.Round(order.Subtotal * 0.08m, 2));

        orderRepo.Add(order);
        await uow.SaveChangesAsync(ct);

        return Result.Success(new PlaceOrderResponse(order.Id, order.OrderNumber, order.Total));
    }
}
