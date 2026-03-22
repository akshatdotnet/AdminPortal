using Common.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Order.Application.DTOs;
using Order.Application.Interfaces;
using Order.Domain.Entities;

namespace Order.Infrastructure.Persistence;

public sealed class OrderRepository(OrderDbContext ctx) : IOrderRepository
{
    public async Task<Order.Domain.Entities.Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await ctx.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IEnumerable<Order.Domain.Entities.Order>> GetByCustomerIdAsync(
        Guid customerId, CancellationToken ct = default)
        => await ctx.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public void Add(Order.Domain.Entities.Order o)    => ctx.Orders.Add(o);
    public void Update(Order.Domain.Entities.Order o) => ctx.Orders.Update(o);
}

public sealed class UnitOfWorkOrder(OrderDbContext ctx) : IUnitOfWorkOrder
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        ctx.SaveChangesAsync(ct);
}

public sealed class OrderQueryService(OrderDbContext ctx)
{
    public async Task<OrderDto?> GetOrderDtoAsync(Guid id, CancellationToken ct)
    {
        var o = await ctx.Orders.AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.StatusHistory)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return o is null ? null : Map(o);
    }

    public async Task<PagedResult<OrderSummaryDto>> GetCustomerOrdersAsync(
        Guid customerId, int page, int size, string? status, CancellationToken ct)
    {
        var q = ctx.Orders.AsNoTracking().Include(o => o.Items)
            .Where(o => o.CustomerId == customerId);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var s))
            q = q.Where(o => o.Status == s);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return PagedResult<OrderSummaryDto>.Create(
            items.Select(ToSummary), total, page, size);
    }

    public async Task<PagedResult<OrderSummaryDto>> GetAllOrdersAsync(
        int page, int size, string? status, CancellationToken ct)
    {
        var q = ctx.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var s))
            q = q.Where(o => o.Status == s);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return PagedResult<OrderSummaryDto>.Create(items.Select(ToSummary), total, page, size);
    }

    private static OrderDto Map(Order.Domain.Entities.Order o) => new(
        o.Id, o.OrderNumber, o.CustomerId,
        o.Status.ToString(), o.PaymentStatus.ToString(),
        new ShippingAddressDto(o.ShippingAddress.FullName, o.ShippingAddress.Street,
            o.ShippingAddress.City, o.ShippingAddress.State, o.ShippingAddress.PostalCode,
            o.ShippingAddress.Country, o.ShippingAddress.Phone),
        o.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Sku,
            i.UnitPrice, i.Quantity, i.LineTotal)),
        o.Subtotal, o.DiscountAmount, o.ShippingCost, o.TaxAmount, o.Total,
        o.CouponCode, o.TrackingNumber, o.CancellationReason, o.Notes,
        o.CreatedAt, o.PaidAt, o.ShippedAt, o.DeliveredAt,
        o.StatusHistory.OrderBy(h => h.Timestamp)
            .Select(h => new StatusHistoryDto(h.Status.ToString(), h.Note, h.Timestamp)));

    private static OrderSummaryDto ToSummary(Order.Domain.Entities.Order o) =>
        new(o.Id, o.OrderNumber, o.Status.ToString(), o.Items.Count, o.Total, o.CreatedAt);
}
