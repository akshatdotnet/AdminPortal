using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;
using AdminPortal.Domain.Entities;

namespace AdminPortal.Application.Interfaces;

public interface ICustomerService
{
    Task<Result<PagedResult<CustomerDto>>> GetCustomersAsync(int page, int pageSize, CustomerType? type = null, string? search = null);
    Task<Result<CustomerDto>> GetCustomerByIdAsync(Guid id);
    Task<Result<CustomerDto>> CreateCustomerAsync(CreateCustomerDto dto);
    Task<Result<int>> GetCountByTypeAsync(CustomerType type);
}
