using System.ComponentModel.DataAnnotations;

namespace WalletSystem.Models;

public class Wallet
{
    public int Id { get; set; }
    public int UserId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Balance { get; set; } = 0;

    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    public WalletStatus Status { get; set; } = WalletStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

public enum WalletStatus { Active, Frozen, Closed }

public class Transaction
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    public int? RelatedWalletId { get; set; } // For transfers

    [Required]
    public TransactionType Type { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    public TransactionStatus Status { get; set; } = TransactionStatus.Completed;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Wallet Wallet { get; set; } = null!;
    public virtual Wallet? RelatedWallet { get; set; }
}

public enum TransactionType
{
    Deposit,
    Withdrawal,
    Transfer,
    Refund,
    Fee,
    Bonus
}

public enum TransactionStatus
{
    Pending,
    Completed,
    Failed,
    Reversed
}
