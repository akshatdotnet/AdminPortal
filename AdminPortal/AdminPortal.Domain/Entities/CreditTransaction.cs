namespace AdminPortal.Domain.Entities;

public class CreditTransaction
{
    public Guid Id { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public decimal Credits { get; set; }
    public TransactionType Type { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum TransactionType { Debit, Credit }
