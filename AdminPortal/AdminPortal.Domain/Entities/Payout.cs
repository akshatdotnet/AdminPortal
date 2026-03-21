namespace AdminPortal.Domain.Entities;

public class Payout
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string BankAccount { get; set; } = string.Empty;
    public PayoutStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string TransactionRef { get; set; } = string.Empty;
}

public enum PayoutStatus { Pending, Processing, Completed, Failed }
