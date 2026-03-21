namespace AdminPortal.Domain.Entities;

public class AnalyticsSummary
{
    public decimal TotalRevenue { get; set; }
    public decimal RevenueChange { get; set; }
    public int TotalOrders { get; set; }
    public int OrdersChange { get; set; }
    public int TotalVisitors { get; set; }
    public int VisitorsChange { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal ConversionChange { get; set; }
    public List<RevenueDataPoint> RevenueChart { get; set; } = new();
    public List<TopProduct> TopProducts { get; set; } = new();
}

public class RevenueDataPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Orders { get; set; }
}

public class TopProduct
{
    public string Name { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
    public decimal Revenue { get; set; }
}
