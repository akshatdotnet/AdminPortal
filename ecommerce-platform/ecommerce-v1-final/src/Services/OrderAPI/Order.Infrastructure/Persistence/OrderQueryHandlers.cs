using Common.Domain.Interfaces;
using Common.Domain.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.Application.DTOs;
using Order.Application.Queries;
using Order.Domain.Entities;

namespace Order.Infrastructure.Persistence;

public sealed class GetOrderByIdQueryHandler(OrderDbContext context)
    : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery q, CancellationToken ct)
    {
        var order = await context.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == q.OrderId, ct);

        if (order is null)
            return Result.Failure<OrderDto>(Error.NotFound("Order", q.OrderId));

        if (!q.IsAdmin && order.CustomerId != q.RequestedByUserId)
            return Result.Failure<OrderDto>(Error.Unauthorized("You cannot view this order."));

        return Result.Success(MapToDto(order));
    }

    private static OrderDto MapToDto(Order.Domain.Entities.Order o) => new(
        o.Id, o.OrderNumber, o.CustomerId,
        o.Status.ToString(), o.PaymentStatus.ToString(),
        new ShippingAddressDto(o.ShippingAddress.FullName, o.ShippingAddress.Street,
            o.ShippingAddress.City, o.ShippingAddress.State, o.ShippingAddress.PostalCode,
            o.ShippingAddress.Country, o.ShippingAddress.Phone),
        o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Sku, i.UnitPrice, i.Quantity, i.LineTotal)),
        o.Subtotal, o.DiscountAmount, o.ShippingCost, o.TaxAmount, o.Total,
        o.CouponCode, o.TrackingNumber, o.CancellationReason, o.Notes,
        o.CreatedAt, o.PaidAt, o.ShippedAt, o.DeliveredAt,
        o.StatusHistory.OrderBy(h => h.Timestamp)
            .Select(h => new OrderStatusHistoryDto(h.Status.ToString(), h.Note, h.Timestamp)));
}

public sealed class GetCustomerOrdersQueryHandler(OrderDbContext context)
    : IRequestHandler<GetCustomerOrdersQuery, Result<PagedResult<OrderSummaryDto>>>
{
    public async Task<Result<PagedResult<OrderSummaryDto>>> Handle(GetCustomerOrdersQuery q, CancellationToken ct)
    {
        var query = context.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.CustomerId == q.CustomerId);

        if (!string.IsNullOrEmpty(q.Status) && Enum.TryParse<OrderStatus>(q.Status, out var status))
            query = query.Where(o => o.Status == status);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(o => o.CreatedAt)
            .Skip((q.PageNumber - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);

        var summaries = items.Select(o => new OrderSummaryDto(
            o.Id, o.OrderNumber, o.Status.ToString(), o.Items.Count, o.Total, o.CreatedAt));

        return Result.Success(PagedResult<OrderSummaryDto>.Create(summaries, total, q.PageNumber, q.PageSize));
    }
}

public sealed class GetAllOrdersQueryHandler(OrderDbContext context)
    : IRequestHandler<GetAllOrdersQuery, Result<PagedResult<OrderSummaryDto>>>
{
    public async Task<Result<PagedResult<OrderSummaryDto>>> Handle(GetAllOrdersQuery q, CancellationToken ct)
    {
        var query = context.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();

        if (!string.IsNullOrEmpty(q.Status) && Enum.TryParse<OrderStatus>(q.Status, out var status))
            query = query.Where(o => o.Status == status);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(o => o.CreatedAt)
            .Skip((q.PageNumber - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);

        var summaries = items.Select(o => new OrderSummaryDto(
            o.Id, o.OrderNumber, o.Status.ToString(), o.Items.Count, o.Total, o.CreatedAt));

        return Result.Success(PagedResult<OrderSummaryDto>.Create(summaries, total, q.PageNumber, q.PageSize));
    }
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public static PagedResult<T> Create(IEnumerable<T> items, int total, int page, int pageSize)
        => new() { Items = items, TotalCount = total, PageNumber = page, PageSize = pageSize };
}
