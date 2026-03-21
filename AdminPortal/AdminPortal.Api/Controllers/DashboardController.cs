using AdminPortal.Api.DTOs;
using AdminPortal.Api.MockData;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly MockDataStore _store;

    public DashboardController(MockDataStore store) => _store = store;

    /// <summary>Returns all KPIs, chart data, top products and recent orders.</summary>
    [HttpGet]
    public ActionResult<ApiResponse<DashboardDto>> GetDashboard()
    {
        // Build last-7-days revenue chart
        var chart = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var day = DateTime.Now.AddDays(-6 + i);
                var revenue = _store.Orders
                    .Where(o => o.OrderDate.Date == day.Date && o.Status != OrderStatus.Cancelled)
                    .Sum(o => o.TotalAmount);
                return new RevenuePointDto { Label = day.ToString("ddd"), Revenue = revenue };
            })
            .ToList();

        var topProducts = _store.Products
            .OrderByDescending(p => p.Price)
            .Take(5)
            .Select(MapProduct)
            .ToList();

        var recentOrders = _store.Orders
            .OrderByDescending(o => o.OrderDate)
            .Take(5)
            .Select(MapOrder)
            .ToList();

        var dto = new DashboardDto
        {
            TotalRevenue = _store.TotalRevenue,
            TotalOrders = _store.TotalOrders,
            TotalProducts = _store.TotalProducts,
            TotalCustomers = _store.Audience.Count,
            RevenueChangePercent = 12.5m,
            OrdersChangePercent = 8,
            RevenueChart = chart,
            TopProducts = topProducts,
            RecentOrders = recentOrders
        };

        return Ok(new ApiResponse<DashboardDto> { Data = dto });
    }

    private static ProductDto MapProduct(Product p) => new()
    {
        Id = p.Id, Name = p.Name, Description = p.Description,
        Price = p.Price, Stock = p.Stock, Category = p.Category,
        ImageUrl = p.ImageUrl, IsActive = p.IsActive, CreatedAt = p.CreatedAt
    };

    private static OrderDto MapOrder(Order o) => new()
    {
        Id = o.Id, OrderNumber = o.OrderNumber, CustomerName = o.CustomerName,
        CustomerPhone = o.CustomerPhone, TotalAmount = o.TotalAmount,
        Status = o.Status.ToString(), OrderDate = o.OrderDate,
        Items = o.Items.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId, ProductName = i.ProductName,
            Quantity = i.Quantity, UnitPrice = i.UnitPrice
        }).ToList()
    };
}
