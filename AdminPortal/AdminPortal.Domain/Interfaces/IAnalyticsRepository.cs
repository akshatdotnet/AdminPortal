using AdminPortal.Domain.Entities;

namespace AdminPortal.Domain.Interfaces;

public interface IAnalyticsRepository
{
    Task<AnalyticsSummary> GetSummaryAsync(DateTime from, DateTime to);
    Task<IEnumerable<RevenueDataPoint>> GetRevenueChartAsync(DateTime from, DateTime to);
}
