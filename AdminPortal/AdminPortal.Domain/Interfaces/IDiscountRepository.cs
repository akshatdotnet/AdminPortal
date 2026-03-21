using AdminPortal.Domain.Entities;

namespace AdminPortal.Domain.Interfaces;

public interface IDiscountRepository : IRepository<Discount>
{
    Task<Discount?> GetByCodeAsync(string code);
    Task<IEnumerable<Discount>> GetActiveDiscountsAsync();
}
