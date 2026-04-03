using Microsoft.EntityFrameworkCore;
using WalletSystem.Data;
using WalletSystem.Models;
using WalletSystem.ViewModels;

namespace WalletSystem.Services;

public interface IWalletService
{
    Task<(bool Success, string Message, Transaction? Txn)> DepositAsync(int walletId, decimal amount, string? description);
    Task<(bool Success, string Message, Transaction? Txn)> WithdrawAsync(int walletId, decimal amount, string? description);
    Task<(bool Success, string Message, Transaction? SenderTxn, Transaction? ReceiverTxn)> TransferAsync(int fromWalletId, string recipientIdentifier, decimal amount, string? description);
    Task<Wallet?> GetWalletByUserIdAsync(int userId);
    Task<Wallet?> GetWalletByIdAsync(int walletId);
    Task<(bool Success, string Message)> FreezeWalletAsync(int walletId);
    Task<(bool Success, string Message)> UnfreezeWalletAsync(int walletId);
}

public class WalletService : IWalletService
{
    private readonly AppDbContext _db;

    public WalletService(AppDbContext db) => _db = db;

    private static string GenerateReference() =>
        $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

    public async Task<(bool, string, Transaction?)> DepositAsync(int walletId, decimal amount, string? description)
    {
        if (amount <= 0) return (false, "Amount must be positive.", null);

        var wallet = await _db.Wallets.FindAsync(walletId);
        if (wallet == null) return (false, "Wallet not found.", null);
        if (wallet.Status != WalletStatus.Active) return (false, $"Wallet is {wallet.Status}. Transactions not allowed.", null);

        var before = wallet.Balance;
        wallet.Balance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        var txn = new Transaction
        {
            WalletId = walletId,
            Type = TransactionType.Deposit,
            Amount = amount,
            BalanceBefore = before,
            BalanceAfter = wallet.Balance,
            Description = description ?? "Deposit",
            ReferenceNumber = GenerateReference(),
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };

        _db.Transactions.Add(txn);
        await _db.SaveChangesAsync();
        return (true, "Deposit successful.", txn);
    }

    public async Task<(bool, string, Transaction?)> WithdrawAsync(int walletId, decimal amount, string? description)
    {
        if (amount <= 0) return (false, "Amount must be positive.", null);

        var wallet = await _db.Wallets.FindAsync(walletId);
        if (wallet == null) return (false, "Wallet not found.", null);
        if (wallet.Status != WalletStatus.Active) return (false, $"Wallet is {wallet.Status}. Transactions not allowed.", null);
        if (wallet.Balance < amount) return (false, $"Insufficient funds. Available: {wallet.Balance:C}", null);

        var before = wallet.Balance;
        wallet.Balance -= amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        var txn = new Transaction
        {
            WalletId = walletId,
            Type = TransactionType.Withdrawal,
            Amount = amount,
            BalanceBefore = before,
            BalanceAfter = wallet.Balance,
            Description = description ?? "Withdrawal",
            ReferenceNumber = GenerateReference(),
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };

        _db.Transactions.Add(txn);
        await _db.SaveChangesAsync();
        return (true, "Withdrawal successful.", txn);
    }

    public async Task<(bool, string, Transaction?, Transaction?)> TransferAsync(int fromWalletId, string recipientIdentifier, decimal amount, string? description)
    {
        if (amount <= 0) return (false, "Amount must be positive.", null, null);

        var fromWallet = await _db.Wallets.Include(w => w.User).FirstOrDefaultAsync(w => w.Id == fromWalletId);
        if (fromWallet == null) return (false, "Source wallet not found.", null, null);
        if (fromWallet.Status != WalletStatus.Active) return (false, $"Source wallet is {fromWallet.Status}.", null, null);
        if (fromWallet.Balance < amount) return (false, $"Insufficient funds. Available: {fromWallet.Balance:C}", null, null);

        var toWallet = await _db.Wallets.Include(w => w.User)
            .FirstOrDefaultAsync(w => w.User.Email == recipientIdentifier || w.User.Username == recipientIdentifier);
        if (toWallet == null) return (false, "Recipient not found.", null, null);
        if (toWallet.Id == fromWalletId) return (false, "Cannot transfer to own wallet.", null, null);
        if (toWallet.Status != WalletStatus.Active) return (false, "Recipient wallet is not active.", null, null);

        var refId = GenerateReference();
        var now = DateTime.UtcNow;

        var senderBefore = fromWallet.Balance;
        fromWallet.Balance -= amount;
        fromWallet.UpdatedAt = now;

        var receiverBefore = toWallet.Balance;
        toWallet.Balance += amount;
        toWallet.UpdatedAt = now;

        var senderTxn = new Transaction
        {
            WalletId = fromWalletId, RelatedWalletId = toWallet.Id,
            Type = TransactionType.Transfer, Amount = amount,
            BalanceBefore = senderBefore, BalanceAfter = fromWallet.Balance,
            Description = description ?? $"Transfer to {toWallet.User.Username}",
            ReferenceNumber = refId, Status = TransactionStatus.Completed, CreatedAt = now
        };

        var receiverTxn = new Transaction
        {
            WalletId = toWallet.Id, RelatedWalletId = fromWalletId,
            Type = TransactionType.Transfer, Amount = amount,
            BalanceBefore = receiverBefore, BalanceAfter = toWallet.Balance,
            Description = description ?? $"Transfer from {fromWallet.User.Username}",
            ReferenceNumber = refId + "-R", Status = TransactionStatus.Completed, CreatedAt = now
        };

        _db.Transactions.AddRange(senderTxn, receiverTxn);
        await _db.SaveChangesAsync();
        return (true, $"Transferred {amount:C} to {toWallet.User.FullName}.", senderTxn, receiverTxn);
    }

    public Task<Wallet?> GetWalletByUserIdAsync(int userId) =>
        _db.Wallets.Include(w => w.User).FirstOrDefaultAsync(w => w.UserId == userId);

    public Task<Wallet?> GetWalletByIdAsync(int walletId) =>
        _db.Wallets.Include(w => w.User).FirstOrDefaultAsync(w => w.Id == walletId);

    public async Task<(bool, string)> FreezeWalletAsync(int walletId)
    {
        var w = await _db.Wallets.FindAsync(walletId);
        if (w == null) return (false, "Not found.");
        w.Status = WalletStatus.Frozen; w.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, "Wallet frozen.");
    }

    public async Task<(bool, string)> UnfreezeWalletAsync(int walletId)
    {
        var w = await _db.Wallets.FindAsync(walletId);
        if (w == null) return (false, "Not found.");
        w.Status = WalletStatus.Active; w.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, "Wallet activated.");
    }
}
