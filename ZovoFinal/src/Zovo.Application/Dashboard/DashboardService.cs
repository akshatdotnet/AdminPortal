using Microsoft.EntityFrameworkCore;
using Zovo.Core.Enums;
using Zovo.Core.Interfaces;

namespace Zovo.Application.Dashboard;

public record DashboardSummary(
    int TotalProducts, int ActiveProducts, int OutOfStockCount, int LowStockCount,
    int TotalOrders, int PendingOrders, int TodayOrders,
    decimal TotalRevenue, decimal MonthRevenue,
    int TotalCustomers, int ActiveCustomers,
    IEnumerable<RecentOrderSummary> RecentOrders,
    IEnumerable<LowStockItem> LowStockItems,
    IEnumerable<MonthlySalesPoint> MonthlySales,
    IEnumerable<CategorySalesPoint> CategoryBreakdown);

public record RecentOrderSummary(int Id, string OrderNumber, string CustomerName,
    decimal TotalAmount, string Status, string PaymentStatus, DateTime CreatedAt);

public record LowStockItem(int Id, string Name, int Stock, int Threshold, string Category);
public record MonthlySalesPoint(string Label, decimal Revenue, int OrderCount);
public record CategorySalesPoint(string Category, int ProductCount, decimal InventoryValue);

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync();
}

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _uow;
    public DashboardService(IUnitOfWork uow) => _uow = uow;

    public async Task<DashboardSummary> GetSummaryAsync()
    {
        var now        = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var products  = await _uow.Products.Query().AsNoTracking().ToListAsync();
        var orders    = await _uow.Orders.Query().AsNoTracking().Include(o => o.Customer).ToListAsync();
        var customers = await _uow.Customers.Query().AsNoTracking().ToListAsync();

        var recentOrders = orders.OrderByDescending(o => o.CreatedAt).Take(6)
            .Select(o => new RecentOrderSummary(o.Id, o.OrderNumber,
                o.Customer is null ? "—" : $"{o.Customer.FirstName} {o.Customer.LastName}",
                o.TotalAmount, o.Status.ToString(), o.PaymentStatus.ToString(), o.CreatedAt));

        var lowStockItems = products.Where(p => p.Stock <= p.LowStockThreshold)
            .OrderBy(p => p.Stock).Take(5)
            .Select(p => new LowStockItem(p.Id, p.Name, p.Stock, p.LowStockThreshold, p.Category));

        var monthly = orders
            .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.CreatedAt >= now.AddMonths(-6))
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlySalesPoint(
                new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                g.Sum(o => o.TotalAmount), g.Count()));

        var catBreakdown = products.GroupBy(p => p.Category)
            .Select(g => new CategorySalesPoint(g.Key, g.Count(), g.Sum(p => p.Price * p.Stock)));

        return new DashboardSummary(
            products.Count, products.Count(p => p.IsActive),
            products.Count(p => p.Stock == 0),
            products.Count(p => p.Stock > 0 && p.Stock <= p.LowStockThreshold),
            orders.Count,
            orders.Count(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed),
            orders.Count(o => o.CreatedAt >= now.Date),
            orders.Where(o => o.PaymentStatus == PaymentStatus.Paid).Sum(o => o.TotalAmount),
            orders.Where(o => o.PaymentStatus == PaymentStatus.Paid && o.CreatedAt >= monthStart).Sum(o => o.TotalAmount),
            customers.Count, customers.Count(c => c.Status == CustomerStatus.Active),
            recentOrders, lowStockItems, monthly, catBreakdown);
    }
}
