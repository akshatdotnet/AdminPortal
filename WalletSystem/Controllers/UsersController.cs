using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WalletSystem.Data;
using WalletSystem.Models;
using WalletSystem.ViewModels;

namespace WalletSystem.Controllers;

public class UsersController : Controller
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db) => _db = db;

    // GET: /Users?search=&status=&page=1
    public async Task<IActionResult> Index(UserFilterVM filter)
    {
        var query = _db.Users.Include(u => u.Wallet).AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(s) ||
                                     u.Email.ToLower().Contains(s) ||
                                     u.Username.ToLower().Contains(s) ||
                                     u.PhoneNumber.Contains(s));
        }

        // Filter
        if (filter.Status.HasValue)
            query = query.Where(u => u.Status == filter.Status.Value);

        // Sort
        query = (filter.SortBy, filter.SortDir) switch
        {
            ("FullName",   "asc")  => query.OrderBy(u => u.FullName),
            ("FullName",   _)      => query.OrderByDescending(u => u.FullName),
            ("Email",      "asc")  => query.OrderBy(u => u.Email),
            ("Email",      _)      => query.OrderByDescending(u => u.Email),
            ("Balance",    "asc")  => query.OrderBy(u => u.Wallet != null ? u.Wallet.Balance : 0),
            ("Balance",    _)      => query.OrderByDescending(u => u.Wallet != null ? u.Wallet.Balance : 0),
            ("CreatedAt",  "asc")  => query.OrderBy(u => u.CreatedAt),
            _                      => query.OrderByDescending(u => u.CreatedAt)
        };

        var total = await query.CountAsync();

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(u => new UserListVM
            {
                Id = u.Id, FullName = u.FullName, Email = u.Email,
                PhoneNumber = u.PhoneNumber, Username = u.Username,
                Status = u.Status, CreatedAt = u.CreatedAt,
                WalletBalance = u.Wallet != null ? u.Wallet.Balance : null,
                WalletStatus  = u.Wallet != null ? u.Wallet.Status  : null
            })
            .ToListAsync();

        ViewBag.Filter = filter;
        ViewBag.Paged = new PagedResult<UserListVM>
        {
            Items = items, TotalCount = total,
            Page = filter.Page, PageSize = filter.PageSize
        };
        return View();
    }

    // GET: /Users/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var user = await _db.Users.Include(u => u.Wallet).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        var txns = await _db.Transactions
            .Where(t => t.WalletId == (user.Wallet != null ? user.Wallet.Id : 0))
            .Include(t => t.RelatedWallet).ThenInclude(w => w!.User)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10).ToListAsync();

        return View(new UserDetailVM { User = user, Wallet = user.Wallet, RecentTransactions = txns });
    }

    // GET: /Users/Create
    public IActionResult Create() => View(new UserCreateVM());

    // POST: /Users/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateVM vm)
    {
        if (await _db.Users.AnyAsync(u => u.Email == vm.Email))
            ModelState.AddModelError("Email", "Email already exists.");
        if (await _db.Users.AnyAsync(u => u.Username == vm.Username))
            ModelState.AddModelError("Username", "Username already taken.");
        if (!ModelState.IsValid) return View(vm);

        var user = new User
        {
            FullName = vm.FullName, Email = vm.Email,
            PhoneNumber = vm.PhoneNumber, Username = vm.Username,
            Status = vm.Status, CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        if (vm.CreateWallet)
        {
            var wallet = new Wallet { UserId = user.Id, Balance = 0, Currency = vm.Currency };
            _db.Wallets.Add(wallet);
            await _db.SaveChangesAsync();

            if (vm.InitialBalance > 0)
            {
                var txn = new Transaction
                {
                    WalletId = wallet.Id, Type = TransactionType.Deposit,
                    Amount = vm.InitialBalance, BalanceBefore = 0, BalanceAfter = vm.InitialBalance,
                    Description = "Initial deposit", Status = TransactionStatus.Completed,
                    ReferenceNumber = $"INIT-{user.Id}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                    CreatedAt = DateTime.UtcNow
                };
                wallet.Balance = vm.InitialBalance;
                _db.Transactions.Add(txn);
                await _db.SaveChangesAsync();
            }
        }

        TempData["Success"] = $"User '{user.FullName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Users/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();
        return View(new UserEditVM { Id = u.Id, FullName = u.FullName, Email = u.Email, PhoneNumber = u.PhoneNumber, Username = u.Username, Status = u.Status });
    }

    // POST: /Users/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserEditVM vm)
    {
        if (id != vm.Id) return BadRequest();
        if (await _db.Users.AnyAsync(u => u.Email == vm.Email && u.Id != id))
            ModelState.AddModelError("Email", "Email already in use.");
        if (await _db.Users.AnyAsync(u => u.Username == vm.Username && u.Id != id))
            ModelState.AddModelError("Username", "Username already taken.");
        if (!ModelState.IsValid) return View(vm);

        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();

        u.FullName = vm.FullName; u.Email = vm.Email;
        u.PhoneNumber = vm.PhoneNumber; u.Username = vm.Username;
        u.Status = vm.Status; u.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = "User updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Users/Delete/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var u = await _db.Users.Include(u => u.Wallet).FirstOrDefaultAsync(u => u.Id == id);
        if (u == null) return NotFound();

        if (u.Wallet != null && u.Wallet.Balance > 0)
        {
            TempData["Error"] = "Cannot delete user with non-zero wallet balance.";
            return RedirectToAction(nameof(Index));
        }

        _db.Users.Remove(u);
        await _db.SaveChangesAsync();
        TempData["Success"] = "User deleted.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Users/ChangeStatus
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, UserStatus status)
    {
        var u = await _db.Users.FindAsync(id);
        if (u == null) return NotFound();
        u.Status = status; u.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"User status changed to {status}.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
