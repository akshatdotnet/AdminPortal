public class DashboardViewModel
{
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public IEnumerable<string> RecentActivities { get; set; } = [];
}
