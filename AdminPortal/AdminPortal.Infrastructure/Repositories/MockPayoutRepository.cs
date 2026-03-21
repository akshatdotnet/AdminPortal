using AdminPortal.Domain.Entities;
using AdminPortal.Domain.Interfaces;
using AdminPortal.Infrastructure.MockData;

namespace AdminPortal.Infrastructure.Repositories;

public class MockPayoutRepository : IPayoutRepository
{
    public Task<Payout?> GetByIdAsync(Guid id) =>
        Task.FromResult(MockDataStore.Payouts.FirstOrDefault(p => p.Id == id));

    public Task<IEnumerable<Payout>> GetAllAsync() =>
        Task.FromResult<IEnumerable<Payout>>(MockDataStore.Payouts.OrderByDescending(p => p.RequestedAt));

    public Task<Payout> AddAsync(Payout entity)
    {
        MockDataStore.Payouts.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<Payout> UpdateAsync(Payout entity)
    {
        var index = MockDataStore.Payouts.FindIndex(p => p.Id == entity.Id);
        if (index >= 0) MockDataStore.Payouts[index] = entity;
        return Task.FromResult(entity);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var payout = MockDataStore.Payouts.FirstOrDefault(p => p.Id == id);
        if (payout is null) return Task.FromResult(false);
        MockDataStore.Payouts.Remove(payout);
        return Task.FromResult(true);
    }

    public Task<IEnumerable<Payout>> GetByStatusAsync(PayoutStatus status) =>
        Task.FromResult<IEnumerable<Payout>>(MockDataStore.Payouts.Where(p => p.Status == status));

    public Task<decimal> GetAvailableBalanceAsync() =>
        Task.FromResult(63250m); // Static mock balance
}
