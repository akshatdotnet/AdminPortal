using Microsoft.EntityFrameworkCore;
using Zovo.Core.Entities;
using Zovo.Core.Enums;
using Zovo.Core.Interfaces;
using Zovo.Core.ValueObjects;

namespace Zovo.Application.Orders;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    public OrderService(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<OrderListItemDto>> GetPagedAsync(OrderQueryParams q)
    {
        // Use IQueryable<Order> — never IIncludableQueryable — so Where/OrderBy reassignment works
        IQueryable<Order> query = _uow.Orders.Query()
            .AsNoTracking()
            .Include(o => o.Customer);

        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(o =>
                o.OrderNumber.Contains(q.Search) ||
                (o.Customer != null && (
                    o.Customer.FirstName.Contains(q.Search) ||
                    o.Customer.LastName.Contains(q.Search) ||
                    o.Customer.Email.Contains(q.Search))));

        if (!string.IsNullOrWhiteSpace(q.Status) && Enum.TryParse<OrderStatus>(q.Status, out var os))
            query = query.Where(o => o.Status == os);

        if (!string.IsNullOrWhiteSpace(q.PaymentStatus) && Enum.TryParse<PaymentStatus>(q.PaymentStatus, out var ps))
            query = query.Where(o => o.PaymentStatus == ps);

        if (q.From.HasValue) query = query.Where(o => o.CreatedAt >= q.From);
        if (q.To.HasValue)   query = query.Where(o => o.CreatedAt <= q.To);

        IQueryable<Order> sorted = q.SortBy switch {
            "oldest"      => query.OrderBy(o => o.CreatedAt),
            "amount_desc" => query.OrderByDescending(o => o.TotalAmount),
            "amount_asc"  => query.OrderBy(o => o.TotalAmount),
            _             => query.OrderByDescending(o => o.CreatedAt)
        };

        var total = await sorted.CountAsync();
        var items = await sorted
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync();

        return PagedResult<OrderListItemDto>.Create(
            items.Select(ToListItem), total, q.Page, q.PageSize);
    }

    public async Task<OrderDetailDto?> GetDetailAsync(int id)
    {
        var o = await _uow.Orders.GetWithDetailsAsync(id);
        return o is null ? null : ToDetail(o);
    }

    public async Task<Result<int>> CreateAsync(CreateOrderCommand cmd)
    {
        await _uow.BeginTransactionAsync();
        try
        {
            var settings = (await _uow.StoreSettings.GetAllAsync()).FirstOrDefault()
                           ?? new StoreSettings();
            decimal subTotal = 0;
            var items = new List<OrderItem>();

            foreach (var line in cmd.Lines)
            {
                var product = await _uow.Products.GetByIdAsync(line.ProductId);
                if (product is null)
                    return Result<int>.Fail($"Product {line.ProductId} not found.");
                if (product.Stock < line.Quantity)
                    return Result<int>.Fail($"Insufficient stock for '{product.Name}'.");

                product.Stock -= line.Quantity;
                await _uow.Products.UpdateAsync(product);
                subTotal += product.Price * line.Quantity;

                items.Add(new OrderItem {
                    ProductId   = product.Id,
                    ProductName = product.Name,
                    ProductSKU  = product.SKU,
                    Quantity    = line.Quantity,
                    UnitPrice   = product.Price
                });
            }

            var shipping  = subTotal >= settings.FreeShippingThreshold
                            ? 0m : settings.DefaultShippingCost;
            var taxAmount = Math.Round(subTotal * settings.TaxRate / 100, 2);

            var order = new Order {
                OrderNumber       = await _uow.Orders.GenerateOrderNumberAsync(),
                CustomerId        = cmd.CustomerId,
                ShippingAddressId = cmd.ShippingAddressId,
                SubTotal          = subTotal,
                ShippingCost      = shipping,
                TaxAmount         = taxAmount,
                TotalAmount       = subTotal + shipping + taxAmount,
                CouponCode        = cmd.CouponCode,
                Notes             = cmd.Notes,
                Items             = items
            };

            await _uow.Orders.AddAsync(order);
            await _uow.SaveChangesAsync();
            await _uow.CommitTransactionAsync();
            return Result<int>.Ok(order.Id, $"Order {order.OrderNumber} created.");
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync();
            return Result<int>.Fail($"Order creation failed: {ex.Message}");
        }
    }

    public async Task<Result> UpdateStatusAsync(int id, OrderStatus status)
    {
        var order = await _uow.Orders.GetByIdAsync(id);
        if (order is null) return Result.Fail("Order not found.");
        order.Status = status;
        if (status == OrderStatus.Shipped   && !order.ShippedAt.HasValue)
            order.ShippedAt   = DateTime.UtcNow;
        if (status == OrderStatus.Delivered && !order.DeliveredAt.HasValue)
            order.DeliveredAt = DateTime.UtcNow;
        await _uow.Orders.UpdateAsync(order);
        await _uow.SaveChangesAsync();
        return Result.Ok($"Order {order.OrderNumber} updated to {status}.");
    }

    public async Task<Result> UpdatePaymentStatusAsync(int id, PaymentStatus status)
    {
        var order = await _uow.Orders.GetByIdAsync(id);
        if (order is null) return Result.Fail("Order not found.");
        order.PaymentStatus = status;
        await _uow.Orders.UpdateAsync(order);
        await _uow.SaveChangesAsync();
        return Result.Ok($"Payment status updated to {status}.");
    }

    public async Task<Result> CancelAsync(int id)
    {
        var order = await _uow.Orders.GetWithDetailsAsync(id);
        if (order is null) return Result.Fail("Order not found.");
        if (order.Status == OrderStatus.Delivered)
            return Result.Fail("Cannot cancel a delivered order.");

        order.Status = OrderStatus.Cancelled;
        foreach (var item in order.Items)
        {
            var p = await _uow.Products.GetByIdAsync(item.ProductId);
            if (p is not null)
            {
                p.Stock += item.Quantity;
                await _uow.Products.UpdateAsync(p);
            }
        }
        await _uow.Orders.UpdateAsync(order);
        await _uow.SaveChangesAsync();
        return Result.Ok($"Order {order.OrderNumber} cancelled and stock restored.");
    }

    // ── Mappers ──────────────────────────────────────────────
    private static OrderListItemDto ToListItem(Order o) => new(
        o.Id, o.OrderNumber,
        o.Customer is null ? "Unknown" : $"{o.Customer.FirstName} {o.Customer.LastName}",
        o.Customer?.Email ?? "",
        o.TotalAmount,
        o.Status.ToString(),
        o.PaymentStatus.ToString(),
        o.CreatedAt);

    private static OrderDetailDto ToDetail(Order o) => new(
        o.Id, o.OrderNumber, o.CustomerId,
        o.Customer is null ? "Unknown" : $"{o.Customer.FirstName} {o.Customer.LastName}",
        o.Customer?.Email ?? "",
        o.Customer?.Phone,
        o.SubTotal, o.ShippingCost, o.Discount, o.TaxAmount, o.TotalAmount,
        o.Status.ToString(), o.PaymentStatus.ToString(),
        o.TrackingNumber, o.Notes, o.CouponCode,
        o.CreatedAt, o.ShippedAt, o.DeliveredAt,
        o.Items.Select(i => new OrderItemDto(
            i.ProductId, i.ProductName, i.ProductSKU,
            i.Quantity, i.UnitPrice, i.Discount, i.LineTotal)));
}
