using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;
using AdminPortal.Application.Interfaces;
using AdminPortal.Domain.Interfaces;

namespace AdminPortal.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _analyticsRepository;

    public AnalyticsService(IAnalyticsRepository analyticsRepository)
    {
        _analyticsRepository = analyticsRepository;
    }

    public async Task<Result<AnalyticsSummaryDto>> GetDashboardSummaryAsync(DateTime from, DateTime to)
    {
        var summary = await _analyticsRepository.GetSummaryAsync(from, to);

        var dto = new AnalyticsSummaryDto
        {
            TotalRevenue = FormatCurrency(summary.TotalRevenue),
            RevenueChange = Math.Abs(summary.RevenueChange),
            RevenueUp = summary.RevenueChange >= 0,
            TotalOrders = summary.TotalOrders,
            OrdersChange = Math.Abs(summary.OrdersChange),
            OrdersUp = summary.OrdersChange >= 0,
            TotalVisitors = summary.TotalVisitors,
            VisitorsChange = Math.Abs(summary.VisitorsChange),
            VisitorsUp = summary.VisitorsChange >= 0,
            ConversionRate = $"{summary.ConversionRate:F1}%",
            ConversionChange = Math.Abs(summary.ConversionChange),
            ConversionUp = summary.ConversionChange >= 0,
            RevenueChart = summary.RevenueChart.Select(d => new ChartDataPointDto
            {
                Label = d.Label,
                Amount = d.Amount,
                Orders = d.Orders
            }).ToList(),
            TopProducts = summary.TopProducts.Select(p => new TopProductDto
            {
                Name = p.Name,
                UnitsSold = p.UnitsSold,
                Revenue = FormatCurrency(p.Revenue)
            }).ToList()
        };

        return Result<AnalyticsSummaryDto>.Success(dto);
    }

    private static string FormatCurrency(decimal amount) =>
        amount >= 1_00_000
            ? $"\u20B9{amount / 1_00_000:F1}L"
            : amount >= 1_000
            ? $"\u20B9{amount / 1_000:F1}K"
            : $"\u20B9{amount:F0}";
}
