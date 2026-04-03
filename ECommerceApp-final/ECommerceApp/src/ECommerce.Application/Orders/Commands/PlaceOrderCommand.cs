using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Orders.Commands;

public record PlaceOrderCommand(
    Guid CustomerId,
    AddressDto ShippingAddress
) : IRequest<Result<OrderDto>>;
