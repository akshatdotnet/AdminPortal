using BookingSystem.Core.Features.Bookings;
using BookingSystem.Core.Interfaces;
using MediatR;

namespace BookingSystem.Core.Features.Bookings;

public class GetOrderHandler(IUnitOfWork uow) : IRequestHandler<GetOrderQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderQuery query, CancellationToken ct)
    {
        var order = await uow.Orders.GetByIdAsync(query.OrderId, ct);
        return order is null ? null
            : new OrderDto(order.Id, order.BookingId, order.Amount, order.Status, order.PaymentReference, order.CreatedAt);
    }
}

public class RefundOrderHandler(IUnitOfWork uow) : IRequestHandler<RefundOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(RefundOrderCommand cmd, CancellationToken ct)
    {
        var order = await uow.Orders.GetByIdAsync(cmd.OrderId, ct)
            ?? throw new KeyNotFoundException($"Order {cmd.OrderId} not found");
        order.Refund();
        await uow.Orders.UpdateAsync(order, ct);
        await uow.SaveChangesAsync(ct);
        return new(order.Id, order.BookingId, order.Amount, order.Status, order.PaymentReference, order.CreatedAt);
    }
}
