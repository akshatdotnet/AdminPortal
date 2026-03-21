using AdminPortal.Domain.Entities;
using AdminPortal.Domain.Interfaces;
using AdminPortal.Infrastructure.MockData;

namespace AdminPortal.Infrastructure.Repositories;

public class MockAnalyticsRepository : IAnalyticsRepository
{
    private static readonly Random _rng = new(42);

    public Task<AnalyticsSummary> GetSummaryAsync(DateTime from, DateTime to)
    {
        var summary = new AnalyticsSummary
        {
            TotalRevenue = 284750m,
            RevenueChange = 18.4m,
            TotalOrders = MockDataStore.Orders.Count,
            OrdersChange = 5,
            TotalVisitors = 12480,
            VisitorsChange = 2340,
            ConversionRate = 3.8m,
            ConversionChange = 0.4m,
            RevenueChart = GenerateChartData(from, to),
            TopProducts = new List<TopProduct>
            {
                new() { Name = "Kaju Katli Premium Box", UnitsSold = 342, Revenue = 246240 },
                new() { Name = "Dry Fruit Gift Hamper", UnitsSold = 89, Revenue = 169011 },
                new() { Name = "Mixed Namkeen 1kg", UnitsSold = 215, Revenue = 60200 },
                new() { Name = "Rose Ladoo Box", UnitsSold = 127, Revenue = 71120 },
                new() { Name = "Badam Milk Powder", UnitsSold = 98, Revenue = 44100 }
            }
        };
        return Task.FromResult(summary);
    }

    public Task<IEnumerable<RevenueDataPoint>> GetRevenueChartAsync(DateTime from, DateTime to)
        => Task.FromResult<IEnumerable<RevenueDataPoint>>(GenerateChartData(from, to));

    private static List<RevenueDataPoint> GenerateChartData(DateTime from, DateTime to)
    {
        var days = (to - from).Days;
        var points = new List<RevenueDataPoint>();
        var current = from;
        var baseRevenue = 8000m;

        while (current <= to)
        {
            var dayOfWeek = (int)current.DayOfWeek;
            var multiplier = dayOfWeek == 0 || dayOfWeek == 6 ? 1.4m : 1.0m;
            var variance = (decimal)(_rng.NextDouble() * 0.4 + 0.8);
            points.Add(new RevenueDataPoint
            {
                Label = days <= 7 ? current.ToString("ddd") : current.ToString("dd MMM"),
                Amount = Math.Round(baseRevenue * multiplier * variance, 0),
                Orders = _rng.Next(8, 35)
            });
            current = current.AddDays(1);
        }
        return points;
    }
}
