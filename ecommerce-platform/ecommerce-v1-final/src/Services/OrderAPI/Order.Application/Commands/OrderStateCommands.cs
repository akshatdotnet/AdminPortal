using Common.Domain.Primitives;
using MediatR;
using Order.Application.Interfaces;

namespace Order.Application.Commands;

public sealed record ConfirmPaymentCommand(Guid OrderId, string PaymentIntentId) : IRequest<Result>;

public sealed class ConfirmPaymentHandler(IOrderRepository repo, IUnitOfWorkOrder uow)
    : IRequestHandler<ConfirmPaymentCommand, Result>
{
    public async Task<Result> Handle(ConfirmPaymentCommand cmd, CancellationToken ct)
    {
        var o = await repo.GetByIdAsync(cmd.OrderId, ct);
        if (o is null) return Result.Failure(Error.NotFound("Order", cmd.OrderId));
        try { o.ConfirmPayment(cmd.PaymentIntentId); repo.Update(o); await uow.SaveChangesAsync(ct); return Result.Success(); }
        catch (InvalidOperationException ex) { return Result.Failure(Error.BusinessRule("Payment", ex.Message)); }
    }
}

public sealed record CancelOrderCommand(Guid OrderId, Guid UserId, string Reason) : IRequest<Result>;

public sealed class CancelOrderHandler(
    IOrderRepository repo, IUnitOfWorkOrder uow, IProductServiceClient productClient)
    : IRequestHandler<CancelOrderCommand, Result>
{
    public async Task<Result> Handle(CancelOrderCommand cmd, CancellationToken ct)
    {
        var o = await repo.GetByIdAsync(cmd.OrderId, ct);
        if (o is null) return Result.Failure(Error.NotFound("Order", cmd.OrderId));
        if (o.CustomerId != cmd.UserId) return Result.Failure(Error.Unauthorized());
        try
        {
            var items = o.Items.ToList();
            o.Cancel(cmd.Reason);
            repo.Update(o);
            await uow.SaveChangesAsync(ct);
            foreach (var item in items)
                await productClient.ReleaseStockAsync(item.ProductId, item.Quantity, ct);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        { return Result.Failure(Error.BusinessRule("Cancel", ex.Message)); }
    }
}

public sealed record ShipOrderCommand(Guid OrderId, string TrackingNumber, string Carrier) : IRequest<Result>;

public sealed class ShipOrderHandler(IOrderRepository repo, IUnitOfWorkOrder uow)
    : IRequestHandler<ShipOrderCommand, Result>
{
    public async Task<Result> Handle(ShipOrderCommand cmd, CancellationToken ct)
    {
        var o = await repo.GetByIdAsync(cmd.OrderId, ct);
        if (o is null) return Result.Failure(Error.NotFound("Order", cmd.OrderId));
        try { o.Ship(cmd.TrackingNumber, cmd.Carrier); repo.Update(o); await uow.SaveChangesAsync(ct); return Result.Success(); }
        catch (InvalidOperationException ex) { return Result.Failure(Error.BusinessRule("Ship", ex.Message)); }
    }
}

public sealed record DeliverOrderCommand(Guid OrderId) : IRequest<Result>;

public sealed class DeliverOrderHandler(IOrderRepository repo, IUnitOfWorkOrder uow)
    : IRequestHandler<DeliverOrderCommand, Result>
{
    public async Task<Result> Handle(DeliverOrderCommand cmd, CancellationToken ct)
    {
        var o = await repo.GetByIdAsync(cmd.OrderId, ct);
        if (o is null) return Result.Failure(Error.NotFound("Order", cmd.OrderId));
        try { o.Deliver(); repo.Update(o); await uow.SaveChangesAsync(ct); return Result.Success(); }
        catch (InvalidOperationException ex) { return Result.Failure(Error.BusinessRule("Deliver", ex.Message)); }
    }
}
