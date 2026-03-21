using AdminPortal.Domain.Entities;

namespace AdminPortal.Application.DTOs;

public class CreditSummaryDto
{
    public decimal CurrentBalance { get; set; }
    public bool IsBalanceLow => CurrentBalance < 2500;
    public List<CreditTransactionDto> Transactions { get; set; } = new();
}

public class CreditTransactionDto
{
    public string ReferenceId { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public decimal Credits { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool IsDebit { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
}
