using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Orders.Commands;

public record CancelOrderCommand(Guid OrderId, Guid CustomerId, string Reason)
    : IRequest<Result<OrderDto>>;
