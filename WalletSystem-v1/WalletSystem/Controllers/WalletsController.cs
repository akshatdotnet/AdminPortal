using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WalletSystem.Data;
using WalletSystem.Models;
using WalletSystem.Services;
using WalletSystem.ViewModels;

namespace WalletSystem.Controllers;

[Authorize]
public class WalletsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWalletService _walletSvc;

    public WalletsController(AppDbContext db, IWalletService walletSvc)
    {
        _db = db;
        _walletSvc = walletSvc;
    }

    // GET: /Wallets
    public async Task<IActionResult> Index(WalletFilterVM filter)
    {
        var query = _db.Wallets.Include(w => w.User)
                               .Include(w => w.Transactions)
                               .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(w => w.User.FullName.ToLower().Contains(s) ||
                                     w.User.Email.ToLower().Contains(s) ||
                                     w.User.Username.ToLower().Contains(s));
        }

        if (filter.Status.HasValue) query = query.Where(w => w.Status == filter.Status);
        if (filter.MinBalance.HasValue) query = query.Where(w => w.Balance >= filter.MinBalance);
        if (filter.MaxBalance.HasValue) query = query.Where(w => w.Balance <= filter.MaxBalance);

        query = (filter.SortBy, filter.SortDir) switch
        {
            ("Balance",   "asc") => query.OrderBy(w => w.Balance),
            ("Balance",   _)     => query.OrderByDescending(w => w.Balance),
            ("OwnerName", "asc") => query.OrderBy(w => w.User.FullName),
            ("OwnerName", _)     => query.OrderByDescending(w => w.User.FullName),
            ("CreatedAt", "asc") => query.OrderBy(w => w.CreatedAt),
            _                    => query.OrderByDescending(w => w.CreatedAt)
        };

        var total = await query.CountAsync();

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(w => new WalletListVM
            {
                Id = w.Id, OwnerName = w.User.FullName, OwnerEmail = w.User.Email,
                Balance = w.Balance, Currency = w.Currency, Status = w.Status,
                CreatedAt = w.CreatedAt, TransactionCount = w.Transactions.Count
            })
            .ToListAsync();

        ViewBag.Filter = filter;
        ViewBag.Paged = new PagedResult<WalletListVM>
        { Items = items, TotalCount = total, Page = filter.Page, PageSize = filter.PageSize };
        return View();
    }

    // GET: /Wallets/Details/5
    public async Task<IActionResult> Details(int id, TransactionFilterVM txnFilter)
    {
        var wallet = await _db.Wallets.Include(w => w.User).FirstOrDefaultAsync(w => w.Id == id);
        if (wallet == null) return NotFound();

        txnFilter.WalletId = id;
        var txnQuery = _db.Transactions.Where(t => t.WalletId == id).AsQueryable();

        if (!string.IsNullOrWhiteSpace(txnFilter.Search))
        {
            var s = txnFilter.Search.ToLower();
            txnQuery = txnQuery.Where(t => (t.Description ?? "").ToLower().Contains(s) ||
                                           (t.ReferenceNumber ?? "").ToLower().Contains(s));
        }
        if (txnFilter.Type.HasValue) txnQuery = txnQuery.Where(t => t.Type == txnFilter.Type);
        if (txnFilter.Status.HasValue) txnQuery = txnQuery.Where(t => t.Status == txnFilter.Status);
        if (txnFilter.DateFrom.HasValue) txnQuery = txnQuery.Where(t => t.CreatedAt >= txnFilter.DateFrom);
        if (txnFilter.DateTo.HasValue)   txnQuery = txnQuery.Where(t => t.CreatedAt <= txnFilter.DateTo.Value.AddDays(1));
        if (txnFilter.MinAmount.HasValue) txnQuery = txnQuery.Where(t => t.Amount >= txnFilter.MinAmount);
        if (txnFilter.MaxAmount.HasValue) txnQuery = txnQuery.Where(t => t.Amount <= txnFilter.MaxAmount);

        txnQuery = txnFilter.SortDir == "asc"
            ? txnQuery.OrderBy(t => t.CreatedAt)
            : txnQuery.OrderByDescending(t => t.CreatedAt);

        var total = await txnQuery.CountAsync();
        var txns = await txnQuery
            .Skip((txnFilter.Page - 1) * txnFilter.PageSize)
            .Take(txnFilter.PageSize)
            .Include(t => t.RelatedWallet).ThenInclude(w => w!.User)
            .Select(t => new TransactionListVM
            {
                Id = t.Id, Type = t.Type, Amount = t.Amount,
                BalanceBefore = t.BalanceBefore, BalanceAfter = t.BalanceAfter,
                Description = t.Description, ReferenceNumber = t.ReferenceNumber,
                Status = t.Status, CreatedAt = t.CreatedAt,
                RelatedOwnerName = t.RelatedWallet != null ? t.RelatedWallet.User.FullName : null
            })
            .ToListAsync();

        ViewBag.Wallet = wallet;
        ViewBag.TxnFilter = txnFilter;
        ViewBag.TxnPaged = new PagedResult<TransactionListVM>
        { Items = txns, TotalCount = total, Page = txnFilter.Page, PageSize = txnFilter.PageSize };
        return View();
    }

    // GET: /Wallets/Deposit/5
    public async Task<IActionResult> Deposit(int id)
    {
        var w = await _walletSvc.GetWalletByIdAsync(id);
        if (w == null) return NotFound();
        return View(new DepositVM { WalletId = w.Id, OwnerName = w.User.FullName, CurrentBalance = w.Balance, Currency = w.Currency });
    }

    // POST: /Wallets/Deposit
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deposit(DepositVM vm)
    {
        if (!ModelState.IsValid) { await RefillDepositVM(vm); return View(vm); }
        var (ok, msg, _) = await _walletSvc.DepositAsync(vm.WalletId, vm.Amount, vm.Description);
        if (ok) { TempData["Success"] = msg; return RedirectToAction(nameof(Details), new { id = vm.WalletId }); }
        ModelState.AddModelError("", msg);
        await RefillDepositVM(vm);
        return View(vm);
    }

    private async Task RefillDepositVM(DepositVM vm)
    {
        var w = await _walletSvc.GetWalletByIdAsync(vm.WalletId);
        if (w != null) { vm.OwnerName = w.User.FullName; vm.CurrentBalance = w.Balance; vm.Currency = w.Currency; }
    }

    // GET: /Wallets/Withdraw/5
    public async Task<IActionResult> Withdraw(int id)
    {
        var w = await _walletSvc.GetWalletByIdAsync(id);
        if (w == null) return NotFound();
        return View(new WithdrawVM { WalletId = w.Id, OwnerName = w.User.FullName, CurrentBalance = w.Balance, Currency = w.Currency });
    }

    // POST: /Wallets/Withdraw
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(WithdrawVM vm)
    {
        if (!ModelState.IsValid) { await RefillWithdrawVM(vm); return View(vm); }
        var (ok, msg, _) = await _walletSvc.WithdrawAsync(vm.WalletId, vm.Amount, vm.Description);
        if (ok) { TempData["Success"] = msg; return RedirectToAction(nameof(Details), new { id = vm.WalletId }); }
        ModelState.AddModelError("", msg);
        await RefillWithdrawVM(vm);
        return View(vm);
    }

    private async Task RefillWithdrawVM(WithdrawVM vm)
    {
        var w = await _walletSvc.GetWalletByIdAsync(vm.WalletId);
        if (w != null) { vm.OwnerName = w.User.FullName; vm.CurrentBalance = w.Balance; vm.Currency = w.Currency; }
    }

    // GET: /Wallets/Transfer/5
    public async Task<IActionResult> Transfer(int id)
    {
        var w = await _walletSvc.GetWalletByIdAsync(id);
        if (w == null) return NotFound();
        return View(new TransferVM { FromWalletId = w.Id, FromOwnerName = w.User.FullName, FromBalance = w.Balance, Currency = w.Currency });
    }

    // POST: /Wallets/Transfer
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Transfer(TransferVM vm)
    {
        if (!ModelState.IsValid) { await RefillTransferVM(vm); return View(vm); }
        var (ok, msg, _, _) = await _walletSvc.TransferAsync(vm.FromWalletId, vm.RecipientIdentifier, vm.Amount, vm.Description);
        if (ok) { TempData["Success"] = msg; return RedirectToAction(nameof(Details), new { id = vm.FromWalletId }); }
        ModelState.AddModelError("", msg);
        await RefillTransferVM(vm);
        return View(vm);
    }

    private async Task RefillTransferVM(TransferVM vm)
    {
        var w = await _walletSvc.GetWalletByIdAsync(vm.FromWalletId);
        if (w != null) { vm.FromOwnerName = w.User.FullName; vm.FromBalance = w.Balance; vm.Currency = w.Currency; }
    }

    // POST: /Wallets/Freeze/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Freeze(int id)
    {
        var (ok, msg) = await _walletSvc.FreezeWalletAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Wallets/Unfreeze/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Unfreeze(int id)
    {
        var (ok, msg) = await _walletSvc.UnfreezeWalletAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }
}
