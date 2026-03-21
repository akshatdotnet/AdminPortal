using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;
using AdminPortal.Application.Interfaces;
using AdminPortal.Domain.Entities;
using AdminPortal.Domain.Interfaces;

namespace AdminPortal.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Result<PagedResult<CustomerDto>>> GetCustomersAsync(
        int page, int pageSize, CustomerType? type = null, string? search = null)
    {
        var customers = string.IsNullOrWhiteSpace(search)
            ? await _customerRepository.GetAllAsync()
            : await _customerRepository.SearchAsync(search);

        if (type.HasValue)
            customers = customers.Where(c => c.Type == type.Value);

        var total = customers.Count();
        var paged = customers
            .OrderByDescending(c => c.TotalSales)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToDto)
            .ToList();

        return Result<PagedResult<CustomerDto>>.Success(new PagedResult<CustomerDto>
        {
            Items = paged,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<CustomerDto>> GetCustomerByIdAsync(Guid id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        return customer is null
            ? Result<CustomerDto>.Failure("Customer not found.")
            : Result<CustomerDto>.Success(MapToDto(customer));
    }

    public async Task<Result<CustomerDto>> CreateCustomerAsync(CreateCustomerDto dto)
    {
        var existing = await _customerRepository.GetByEmailAsync(dto.Email);
        if (existing is not null)
            return Result<CustomerDto>.Failure("A customer with this email already exists.");

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            MobileNumber = dto.MobileNumber,
            Email = dto.Email,
            City = dto.City,
            State = dto.State,
            Type = CustomerType.New,
            TotalOrders = 0,
            TotalSales = 0,
            CreatedAt = DateTime.UtcNow
        };
        var created = await _customerRepository.AddAsync(customer);
        return Result<CustomerDto>.Success(MapToDto(created));
    }

    public async Task<Result<int>> GetCountByTypeAsync(CustomerType type)
    {
        var count = await _customerRepository.GetCountByTypeAsync(type);
        return Result<int>.Success(count);
    }

    private static CustomerDto MapToDto(Customer c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        MobileNumber = c.MobileNumber,
        Email = c.Email,
        City = c.City,
        State = c.State,
        Type = c.Type.ToString(),
        TypeEnum = c.Type,
        TotalOrders = c.TotalOrders,
        TotalSales = $"\u20B9{c.TotalSales:N0}",
        CreatedAt = c.CreatedAt,
        LastOrderAt = c.LastOrderAt
    };
}
