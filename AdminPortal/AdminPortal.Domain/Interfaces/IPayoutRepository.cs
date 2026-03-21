using AdminPortal.Domain.Entities;

namespace AdminPortal.Domain.Interfaces;

public interface IPayoutRepository : IRepository<Payout>
{
    Task<IEnumerable<Payout>> GetByStatusAsync(PayoutStatus status);
    Task<decimal> GetAvailableBalanceAsync();
}
