using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;

namespace AdminPortal.Application.Interfaces;

public interface IAnalyticsService
{
    Task<Result<AnalyticsSummaryDto>> GetDashboardSummaryAsync(DateTime from, DateTime to);
}
