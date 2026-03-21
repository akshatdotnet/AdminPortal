namespace AdminPortal.Application.DTOs;

public class PayoutDto
{
    public Guid Id { get; set; }
    public string Amount { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string TransactionRef { get; set; } = string.Empty;
}

public class PayoutSummaryDto
{
    public string AvailableBalance { get; set; } = string.Empty;
    public string TotalEarned { get; set; } = string.Empty;
    public string PendingAmount { get; set; } = string.Empty;
    public List<PayoutDto> RecentPayouts { get; set; } = new();
}
