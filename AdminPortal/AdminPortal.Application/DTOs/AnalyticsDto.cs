namespace AdminPortal.Application.DTOs;

public class AnalyticsSummaryDto
{
    public string TotalRevenue { get; set; } = string.Empty;
    public decimal RevenueChange { get; set; }
    public bool RevenueUp { get; set; }
    public int TotalOrders { get; set; }
    public int OrdersChange { get; set; }
    public bool OrdersUp { get; set; }
    public int TotalVisitors { get; set; }
    public int VisitorsChange { get; set; }
    public bool VisitorsUp { get; set; }
    public string ConversionRate { get; set; } = string.Empty;
    public decimal ConversionChange { get; set; }
    public bool ConversionUp { get; set; }
    public List<ChartDataPointDto> RevenueChart { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
}

public class ChartDataPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Orders { get; set; }
}

public class TopProductDto
{
    public string Name { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
    public string Revenue { get; set; } = string.Empty;
}
