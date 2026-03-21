using AdminPortal.Domain.Entities;

namespace AdminPortal.Domain.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<IEnumerable<Customer>> GetByTypeAsync(CustomerType type);
    Task<IEnumerable<Customer>> SearchAsync(string query);
    Task<Customer?> GetByEmailAsync(string email);
    Task<int> GetCountByTypeAsync(CustomerType type);
}
