using ECommerce.Application.Common.Models;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Orders.Queries;

public record GetOrdersQuery(Guid CustomerId) : IRequest<Result<IReadOnlyList<OrderDto>>>;

public class GetOrdersQueryHandler(IOrderRepository orders)
    : IRequestHandler<GetOrdersQuery, Result<IReadOnlyList<OrderDto>>>
{
    public async Task<Result<IReadOnlyList<OrderDto>>> Handle(GetOrdersQuery query, CancellationToken ct)
    {
        var list = await orders.GetByCustomerIdAsync(query.CustomerId, ct);
        var dtos = list.Select(o => new OrderDto(
            o.Id, o.OrderNumber, o.CustomerId, o.Status.ToString(),
            o.TotalAmount.Amount, o.TotalAmount.Currency,
            o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.UnitPrice.Amount, i.Quantity, i.Subtotal.Amount)).ToList(),
            new AddressDto(o.ShippingAddress.Street, o.ShippingAddress.City, o.ShippingAddress.State, o.ShippingAddress.PinCode, o.ShippingAddress.Country),
            o.CreatedAt, o.TrackingNumber)).ToList();

        return Result.Success<IReadOnlyList<OrderDto>>(dtos);
    }
}
