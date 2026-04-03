using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WalletSystem.Data;
using WalletSystem.Models;
using WalletSystem.ViewModels;

namespace WalletSystem.Controllers;

public class TransactionsController : Controller
{
    private readonly AppDbContext _db;
    public TransactionsController(AppDbContext db) => _db = db;

    // GET: /Transactions
    public async Task<IActionResult> Index(TransactionFilterVM filter)
    {
        var query = _db.Transactions
            .Include(t => t.Wallet).ThenInclude(w => w.User)
            .Include(t => t.RelatedWallet).ThenInclude(w => w!.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.ToLower();
            query = query.Where(t =>
                (t.ReferenceNumber ?? "").ToLower().Contains(s) ||
                (t.Description ?? "").ToLower().Contains(s) ||
                t.Wallet.User.FullName.ToLower().Contains(s) ||
                t.Wallet.User.Username.ToLower().Contains(s));
        }

        if (filter.Type.HasValue)   query = query.Where(t => t.Type == filter.Type);
        if (filter.Status.HasValue) query = query.Where(t => t.Status == filter.Status);
        if (filter.DateFrom.HasValue) query = query.Where(t => t.CreatedAt >= filter.DateFrom);
        if (filter.DateTo.HasValue)   query = query.Where(t => t.CreatedAt <= filter.DateTo.Value.AddDays(1));
        if (filter.MinAmount.HasValue) query = query.Where(t => t.Amount >= filter.MinAmount);
        if (filter.MaxAmount.HasValue) query = query.Where(t => t.Amount <= filter.MaxAmount);

        query = filter.SortDir == "asc"
            ? query.OrderBy(t => t.CreatedAt)
            : query.OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new TransactionListVM
            {
                Id = t.Id, OwnerName = t.Wallet.User.FullName,
                Type = t.Type, Amount = t.Amount,
                BalanceBefore = t.BalanceBefore, BalanceAfter = t.BalanceAfter,
                Description = t.Description, ReferenceNumber = t.ReferenceNumber,
                Status = t.Status, CreatedAt = t.CreatedAt,
                RelatedOwnerName = t.RelatedWallet != null ? t.RelatedWallet.User.FullName : null
            }).ToListAsync();

        ViewBag.Filter = filter;
        ViewBag.Paged = new PagedResult<TransactionListVM>
        { Items = items, TotalCount = total, Page = filter.Page, PageSize = filter.PageSize };
        return View();
    }

    // GET: /Transactions/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var txn = await _db.Transactions
            .Include(t => t.Wallet).ThenInclude(w => w.User)
            .Include(t => t.RelatedWallet).ThenInclude(w => w!.User)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (txn == null) return NotFound();
        return View(txn);
    }
}

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    //public async Task<IActionResult> Index()
    //{
    //    var today = DateTime.UtcNow.Date;
    //    var vm = new DashboardVM
    //    {
    //        TotalUsers       = await _db.Users.CountAsync(),
    //        ActiveWallets    = await _db.Wallets.CountAsync(w => w.Status == WalletStatus.Active),
    //        TotalBalance     = await _db.Wallets.SumAsync(w => w.Balance),
    //        TodayTransactions = await _db.Transactions.CountAsync(t => t.CreatedAt >= today),
    //        TodayVolume      = await _db.Transactions.Where(t => t.CreatedAt >= today && t.Status == TransactionStatus.Completed).SumAsync(t => t.Amount),
    //        RecentTransactions = await _db.Transactions
    //            .Include(t => t.Wallet).ThenInclude(w => w.User)
    //            .OrderByDescending(t => t.CreatedAt)
    //            .Take(8)
    //            .Select(t => new TransactionListVM
    //            {
    //                Id = t.Id, OwnerName = t.Wallet.User.FullName,
    //                Type = t.Type, Amount = t.Amount,
    //                Description = t.Description, Status = t.Status,
    //                CreatedAt = t.CreatedAt
    //            }).ToListAsync()
    //    };
    //    return View(vm);
    //}

    public async Task<IActionResult> Index()
    {
        var today = DateTime.UtcNow.Date;

        var vm = new DashboardVM
        {
            TotalUsers = await _db.Users.CountAsync(),
            ActiveWallets = await _db.Wallets.CountAsync(w => w.Status == WalletStatus.Active),

            // Cast to double in the SQL projection — SQLite supports SUM on REAL/double
            TotalBalance = (decimal)await _db.Wallets
                                    .Select(w => (double)w.Balance)
                                    .SumAsync(),

            TodayTransactions = await _db.Transactions
                                    .CountAsync(t => t.CreatedAt >= today),

            TodayVolume = (decimal)await _db.Transactions
                                    .Where(t => t.CreatedAt >= today
                                             && t.Status == TransactionStatus.Completed)
                                    .Select(t => (double)t.Amount)
                                    .SumAsync(),

            RecentTransactions = await _db.Transactions
                .Include(t => t.Wallet).ThenInclude(w => w.User)
                .OrderByDescending(t => t.CreatedAt)
                .Take(8)
                .Select(t => new TransactionListVM
                {
                    Id = t.Id,
                    OwnerName = t.Wallet.User.FullName,
                    Type = t.Type,
                    Amount = t.Amount,
                    Description = t.Description,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync()
        };

        return View(vm);
    }
}
