using AdminPortal.Domain.Entities;

namespace AdminPortal.Domain.Interfaces;

public interface ICreditRepository
{
    Task<decimal> GetCurrentBalanceAsync();
    Task<IEnumerable<CreditTransaction>> GetAllAsync();
    Task<IEnumerable<CreditTransaction>> GetByTypeAsync(TransactionType type);
    Task<IEnumerable<CreditTransaction>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<CreditTransaction> AddAsync(CreditTransaction transaction);
}
