using AdminPortal.Domain.Entities;
using AdminPortal.Domain.Interfaces;
using AdminPortal.Infrastructure.MockData;

namespace AdminPortal.Infrastructure.Repositories;

public class MockCustomerRepository : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(Guid id) =>
        Task.FromResult(MockDataStore.Customers.FirstOrDefault(c => c.Id == id));

    public Task<IEnumerable<Customer>> GetAllAsync() =>
        Task.FromResult<IEnumerable<Customer>>(MockDataStore.Customers.OrderByDescending(c => c.TotalSales));

    public Task<Customer> AddAsync(Customer entity)
    {
        MockDataStore.Customers.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<Customer> UpdateAsync(Customer entity)
    {
        var index = MockDataStore.Customers.FindIndex(c => c.Id == entity.Id);
        if (index >= 0) MockDataStore.Customers[index] = entity;
        return Task.FromResult(entity);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var customer = MockDataStore.Customers.FirstOrDefault(c => c.Id == id);
        if (customer is null) return Task.FromResult(false);
        MockDataStore.Customers.Remove(customer);
        return Task.FromResult(true);
    }

    public Task<IEnumerable<Customer>> GetByTypeAsync(CustomerType type) =>
        Task.FromResult<IEnumerable<Customer>>(MockDataStore.Customers.Where(c => c.Type == type));

    public Task<IEnumerable<Customer>> SearchAsync(string query) =>
        Task.FromResult<IEnumerable<Customer>>(MockDataStore.Customers.Where(c =>
            c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            c.MobileNumber.Contains(query) ||
            c.City.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            c.Email.Contains(query, StringComparison.OrdinalIgnoreCase)));

    public Task<Customer?> GetByEmailAsync(string email) =>
        Task.FromResult(MockDataStore.Customers.FirstOrDefault(c =>
            c.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public Task<int> GetCountByTypeAsync(CustomerType type) =>
        Task.FromResult(MockDataStore.Customers.Count(c => c.Type == type));
}
