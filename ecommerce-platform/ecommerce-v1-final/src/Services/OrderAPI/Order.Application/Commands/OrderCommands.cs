using Common.Domain.Primitives;
using FluentValidation;
using MediatR;
using Order.Application.DTOs;
using Order.Application.Interfaces;
using Order.Domain.Entities;

namespace Order.Application.Commands;

// ══════════════════════════════════════════════════════════════
// PLACE ORDER — orchestrates: validate cart → reserve stock
//               → apply coupon → create order → trigger payment
// ══════════════════════════════════════════════════════════════
public sealed record PlaceOrderCommand(
    Guid CustomerId,
    string ShippingFullName,
    string ShippingStreet,
    string ShippingCity,
    string ShippingState,
    string ShippingPostalCode,
    string ShippingCountry,
    string ShippingPhone,
    IReadOnlyList<OrderItemRequest> Items,
    string? CouponCode,
    string? Notes) : IRequest<Result<PlaceOrderResponse>>;

public sealed record OrderItemRequest(
    Guid ProductId,
    string ProductName,
    string Sku,
    decimal UnitPrice,
    int Quantity);

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
        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must contain at least one item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0).LessThanOrEqualTo(100);
            item.RuleFor(i => i.UnitPrice).GreaterThan(0);
        });
    }
}

public sealed class PlaceOrderCommandHandler(
    IOrderRepository orderRepo,
    IUnitOfWorkOrder uow,
    IProductServiceClient productClient,
    ICouponServiceClient couponClient) :
    IRequestHandler<PlaceOrderCommand, Result<PlaceOrderResponse>>
{
    public async Task<Result<PlaceOrderResponse>> Handle(
        PlaceOrderCommand cmd, CancellationToken ct)
    {
        // Step 1: Validate product availability and reserve stock
        foreach (var item in cmd.Items)
        {
            var stockResult = await productClient.ReserveStockAsync(item.ProductId, item.Quantity, ct);
            if (!stockResult.IsSuccess)
                return Result.Failure<PlaceOrderResponse>(stockResult.Error);
        }

        // Step 2: Create order aggregate
        var address = new ShippingAddress(
            cmd.ShippingFullName, cmd.ShippingStreet, cmd.ShippingCity,
            cmd.ShippingState, cmd.ShippingPostalCode, cmd.ShippingCountry, cmd.ShippingPhone);

        var order = Order.Domain.Entities.Order.Create(cmd.CustomerId, address, cmd.Notes);

        foreach (var item in cmd.Items)
            order.AddItem(item.ProductId, item.ProductName, item.Sku,
                item.UnitPrice, "USD", item.Quantity);

        // Step 3: Apply coupon if provided
        if (!string.IsNullOrEmpty(cmd.CouponCode))
        {
            var couponResult = await couponClient.ValidateAndApplyAsync(
                cmd.CouponCode, order.Subtotal.Amount, ct);

            if (couponResult.IsSuccess)
                order.ApplyCoupon(cmd.CouponCode, couponResult.Value.DiscountAmount, "USD");
        }

        // Step 4: Calculate shipping (flat-rate for simplicity; hook in real calculator)
        order.SetShipping(9.99m, "USD");

        // Step 5: Calculate tax (simplified — use Avalara/TaxJar in production)
        var taxRate = 0.08m;
        order.SetTax(Math.Round(order.Subtotal.Amount * taxRate, 2), "USD");

        orderRepo.Add(order);
        await uow.SaveChangesAsync(ct);

        return Result.Success(new PlaceOrderResponse(order.Id, order.OrderNumber, order.Total.Amount));
    }
}

// ══════════════════════════════════════════════════════════════
// CONFIRM PAYMENT — called by PaymentAPI webhook
// ══════════════════════════════════════════════════════════════
public sealed record ConfirmPaymentCommand(
    Guid OrderId,
    string PaymentIntentId) : IRequest<Result>;

public sealed class ConfirmPaymentCommandHandler(
    IOrderRepository orderRepo,
    IUnitOfWorkOrder uow) :
    IRequestHandler<ConfirmPaymentCommand, Result>
{
    public async Task<Result> Handle(ConfirmPaymentCommand cmd, CancellationToken ct)
    {
        var order = await orderRepo.GetByIdAsync(cmd.OrderId, ct);
        if (order is null)
            return Result.Failure(Error.NotFound("Order", cmd.OrderId));

        try
        {
            order.ConfirmPayment(cmd.PaymentIntentId);
            orderRepo.Update(order);
            await uow.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.BusinessRule("PaymentConfirmation", ex.Message));
        }
    }
}

// ══════════════════════════════════════════════════════════════
// CANCEL ORDER
// ══════════════════════════════════════════════════════════════
public sealed record CancelOrderCommand(
    Guid OrderId,
    Guid RequestedByUserId,
    string Reason) : IRequest<Result>;

public sealed class CancelOrderCommandHandler(
    IOrderRepository orderRepo,
    IUnitOfWorkOrder uow,
    IProductServiceClient productClient) :
    IRequestHandler<CancelOrderCommand, Result>
{
    public async Task<Result> Handle(CancelOrderCommand cmd, CancellationToken ct)
    {
        var order = await orderRepo.GetByIdAsync(cmd.OrderId, ct);
        if (order is null)
            return Result.Failure(Error.NotFound("Order", cmd.OrderId));

        if (order.CustomerId != cmd.RequestedByUserId)
            return Result.Failure(Error.Unauthorized("You cannot cancel this order."));

        try
        {
            var itemsToRestore = order.Items.ToList();
            order.Cancel(cmd.Reason);
            orderRepo.Update(order);
            await uow.SaveChangesAsync(ct);

            // Release reserved stock
            foreach (var item in itemsToRestore)
                await productClient.ReleaseStockAsync(item.ProductId, item.Quantity, ct);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.BusinessRule("OrderCancellation", ex.Message));
        }
    }
}

// ══════════════════════════════════════════════════════════════
// SHIP ORDER
// ══════════════════════════════════════════════════════════════
public sealed record ShipOrderCommand(
    Guid OrderId,
    string TrackingNumber,
    string Carrier) : IRequest<Result>;

public sealed class ShipOrderCommandHandler(
    IOrderRepository orderRepo,
    IUnitOfWorkOrder uow) :
    IRequestHandler<ShipOrderCommand, Result>
{
    public async Task<Result> Handle(ShipOrderCommand cmd, CancellationToken ct)
    {
        var order = await orderRepo.GetByIdAsync(cmd.OrderId, ct);
        if (order is null) return Result.Failure(Error.NotFound("Order", cmd.OrderId));

        try
        {
            order.Ship(cmd.TrackingNumber, cmd.Carrier);
            orderRepo.Update(order);
            await uow.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.BusinessRule("ShipOrder", ex.Message));
        }
    }
}

// ── Supporting types ──────────────────────────────────────────
public sealed record PlaceOrderResponse(Guid OrderId, string OrderNumber, decimal Total);
