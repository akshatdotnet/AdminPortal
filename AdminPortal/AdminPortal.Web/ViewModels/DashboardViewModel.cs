using AdminPortal.Application.DTOs;

namespace AdminPortal.Web.ViewModels;

public class DashboardViewModel
{
    public AnalyticsSummaryDto Analytics { get; set; } = new();
    public List<OrderDto> RecentOrders { get; set; } = new();
    public string StoreName { get; set; } = string.Empty;
    public bool StoreIsOpen { get; set; }
    public string SelectedPeriod { get; set; } = "7d";
}
