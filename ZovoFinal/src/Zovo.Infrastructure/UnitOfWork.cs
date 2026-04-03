using Microsoft.EntityFrameworkCore.Storage;
using Zovo.Core.Entities;
using Zovo.Core.Interfaces;
using Zovo.Infrastructure.Data;
using Zovo.Infrastructure.Repositories;

namespace Zovo.Infrastructure;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ZovoDbContext _db;
    private IDbContextTransaction? _tx;

    public IProductRepository  Products      { get; }
    public IOrderRepository    Orders        { get; }
    public ICustomerRepository Customers     { get; }
    public IRepository<StoreSettings> StoreSettings { get; }

    public UnitOfWork(ZovoDbContext db)
    {
        _db           = db;
        Products      = new ProductRepository(db);
        Orders        = new OrderRepository(db);
        Customers     = new CustomerRepository(db);
        StoreSettings = new Repository<StoreSettings>(db);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync()
        => _tx = await _db.Database.BeginTransactionAsync();

    public async Task CommitTransactionAsync()
    {
        if (_tx is not null) { await _tx.CommitAsync(); await _tx.DisposeAsync(); _tx = null; }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_tx is not null) { await _tx.RollbackAsync(); await _tx.DisposeAsync(); _tx = null; }
    }

    public void Dispose() => _tx?.Dispose();
}
