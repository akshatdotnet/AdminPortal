using Common.Domain.Interfaces;
using Common.Domain.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.Application.DTOs;
using Order.Application.Interfaces;
using Order.Domain.Entities;

namespace Order.Application.Queries;

public sealed record GetOrderByIdQuery(Guid OrderId, Guid RequestedByUserId, bool IsAdmin = false)
    : IRequest<Result<OrderDto>>;

public sealed record GetCustomerOrdersQuery(Guid CustomerId, int PageNumber = 1, int PageSize = 10, string? Status = null)
    : IRequest<Result<PagedResult<OrderSummaryDto>>>;

public sealed record GetAllOrdersQuery(int PageNumber = 1, int PageSize = 20, string? Status = null)
    : IRequest<Result<PagedResult<OrderSummaryDto>>>;
