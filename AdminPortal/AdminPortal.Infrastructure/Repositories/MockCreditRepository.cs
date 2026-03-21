using AdminPortal.Domain.Entities;
using AdminPortal.Domain.Interfaces;
using AdminPortal.Infrastructure.MockData;

namespace AdminPortal.Infrastructure.Repositories;

public class MockCreditRepository : ICreditRepository
{
    public Task<decimal> GetCurrentBalanceAsync() =>
        Task.FromResult(MockDataStore.CreditTransactions
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefault()?.Balance ?? 0m);

    public Task<IEnumerable<CreditTransaction>> GetAllAsync() =>
        Task.FromResult<IEnumerable<CreditTransaction>>(
            MockDataStore.CreditTransactions.OrderByDescending(t => t.CreatedAt));

    public Task<IEnumerable<CreditTransaction>> GetByTypeAsync(TransactionType type) =>
        Task.FromResult<IEnumerable<CreditTransaction>>(
            MockDataStore.CreditTransactions.Where(t => t.Type == type).OrderByDescending(t => t.CreatedAt));

    public Task<IEnumerable<CreditTransaction>> GetByDateRangeAsync(DateTime from, DateTime to) =>
        Task.FromResult<IEnumerable<CreditTransaction>>(
            MockDataStore.CreditTransactions
                .Where(t => t.CreatedAt >= from && t.CreatedAt <= to)
                .OrderByDescending(t => t.CreatedAt));

    public Task<CreditTransaction> AddAsync(CreditTransaction transaction)
    {
        MockDataStore.CreditTransactions.Add(transaction);
        return Task.FromResult(transaction);
    }
}
