using System.ComponentModel.DataAnnotations;
using WalletSystem.Models;

namespace WalletSystem.ViewModels;

// ─── Paging / Sorting base ───────────────────────────────────────────────────
public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

// ─── User ViewModels ──────────────────────────────────────────────────────────
public class UserListVM
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Username { get; set; } = "";
    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal? WalletBalance { get; set; }
    public WalletStatus? WalletStatus { get; set; }
}

public class UserFilterVM
{
    public string? Search { get; set; }
    public UserStatus? Status { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public string SortDir { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class UserCreateVM
{
    [Required, MaxLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = "";

    [Required, Phone, MaxLength(20)]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = "";

    [Required, MaxLength(50)]
    public string Username { get; set; } = "";

    public UserStatus Status { get; set; } = UserStatus.Active;

    [Display(Name = "Create Wallet")]
    public bool CreateWallet { get; set; } = true;

    [Display(Name = "Initial Balance")]
    [Range(0, double.MaxValue)]
    public decimal InitialBalance { get; set; } = 0;

    [MaxLength(3)]
    public string Currency { get; set; } = "USD";
}

public class UserEditVM
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = "";

    [Required, Phone, MaxLength(20)]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = "";

    [Required, MaxLength(50)]
    public string Username { get; set; } = "";

    public UserStatus Status { get; set; }
}

public class UserDetailVM
{
    public User User { get; set; } = null!;
    public Wallet? Wallet { get; set; }
    public List<Transaction> RecentTransactions { get; set; } = [];
}

// ─── Wallet ViewModels ────────────────────────────────────────────────────────
public class WalletListVM
{
    public int Id { get; set; }
    public string OwnerName { get; set; } = "";
    public string OwnerEmail { get; set; } = "";
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "";
    public WalletStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TransactionCount { get; set; }
}

public class WalletFilterVM
{
    public string? Search { get; set; }
    public WalletStatus? Status { get; set; }
    public decimal? MinBalance { get; set; }
    public decimal? MaxBalance { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public string SortDir { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class DepositVM
{
    public int WalletId { get; set; }
    public string OwnerName { get; set; } = "";
    public decimal CurrentBalance { get; set; }
    public string Currency { get; set; } = "USD";

    [Required]
    [Range(0.01, 1000000, ErrorMessage = "Amount must be between 0.01 and 1,000,000")]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class WithdrawVM
{
    public int WalletId { get; set; }
    public string OwnerName { get; set; } = "";
    public decimal CurrentBalance { get; set; }
    public string Currency { get; set; } = "USD";

    [Required]
    [Range(0.01, 1000000, ErrorMessage = "Amount must be between 0.01 and 1,000,000")]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class TransferVM
{
    public int FromWalletId { get; set; }
    public string FromOwnerName { get; set; } = "";
    public decimal FromBalance { get; set; }
    public string Currency { get; set; } = "USD";

    [Required]
    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Recipient username or email is required")]
    [Display(Name = "Recipient (username or email)")]
    public string RecipientIdentifier { get; set; } = "";

    [MaxLength(500)]
    public string? Description { get; set; }
}

// ─── Transaction ViewModels ───────────────────────────────────────────────────
public class TransactionFilterVM
{
    public int? WalletId { get; set; }
    public string? Search { get; set; }
    public TransactionType? Type { get; set; }
    public TransactionStatus? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public string SortDir { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class TransactionListVM
{
    public int Id { get; set; }
    public string OwnerName { get; set; } = "";
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
    public string? ReferenceNumber { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RelatedOwnerName { get; set; }
}

public class DashboardVM
{
    public int TotalUsers { get; set; }
    public int ActiveWallets { get; set; }
    public decimal TotalBalance { get; set; }
    public int TodayTransactions { get; set; }
    public decimal TodayVolume { get; set; }
    public List<TransactionListVM> RecentTransactions { get; set; } = [];
}
