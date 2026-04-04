using Microsoft.EntityFrameworkCore;
using Zovo.Core.Entities;
using Zovo.Core.Enums;
using Zovo.Core.Interfaces;
using Zovo.Core.ValueObjects;

namespace Zovo.Application.Customers;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _uow;
    public CustomerService(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<CustomerListItemDto>> GetPagedAsync(CustomerQueryParams q)
    {
        // IQueryable<Customer> — not IIncludableQueryable — allows safe reassignment
        IQueryable<Customer> query = _uow.Customers.Query()
            .AsNoTracking()
            .Include(c => c.Orders);

        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(c =>
                c.FirstName.Contains(q.Search) ||
                c.LastName.Contains(q.Search)  ||
                c.Email.Contains(q.Search));

        if (!string.IsNullOrWhiteSpace(q.Status) &&
            Enum.TryParse<CustomerStatus>(q.Status, out var cs))
            query = query.Where(c => c.Status == cs);

        IQueryable<Customer> sorted = q.SortBy switch {
            "name_asc"    => query.OrderBy(c => c.FirstName),
            "orders_desc" => query.OrderByDescending(c => c.Orders.Count),
            _             => query.OrderByDescending(c => c.CreatedAt)
        };

        var total = await sorted.CountAsync();
        var items = await sorted
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync();

        return PagedResult<CustomerListItemDto>.Create(
            items.Select(c => new CustomerListItemDto(
                c.Id,
                $"{c.FirstName} {c.LastName}",
                c.Email,
                c.Phone,
                c.Status.ToString(),
                c.Orders.Count,
                c.Orders
                    .Where(o => o.PaymentStatus == PaymentStatus.Paid)
                    .Sum(o => o.TotalAmount),
                c.CreatedAt)),
            total, q.Page, q.PageSize);
    }

    public async Task<CustomerDetailDto?> GetDetailAsync(int id)
    {
        var c = await _uow.Customers.GetWithOrdersAsync(id);
        if (c is null) return null;
        return new CustomerDetailDto(
            c.Id, c.FirstName, c.LastName, c.Email, c.Phone,
            c.AvatarUrl, c.Status.ToString(), c.Notes,
            c.Orders.Count,
            c.Orders.Where(o => o.PaymentStatus == PaymentStatus.Paid)
                    .Sum(o => o.TotalAmount),
            c.CreatedAt,
            c.Addresses.Select(a => new CustomerAddressDto(
                a.Id, a.Line1, a.Line2,
                a.City, a.State, a.PostalCode, a.Country, a.IsDefault)));
    }

    public async Task<Result<int>> CreateAsync(CreateCustomerCommand cmd)
    {
        if (await _uow.Customers.GetByEmailAsync(cmd.Email) is not null)
            return Result<int>.Fail("Email already registered.", "DUPLICATE_EMAIL");

        var c = new Customer {
            FirstName = cmd.FirstName,
            LastName  = cmd.LastName,
            Email     = cmd.Email,
            Phone     = cmd.Phone,
            Notes     = cmd.Notes
        };
        await _uow.Customers.AddAsync(c);
        await _uow.SaveChangesAsync();
        return Result<int>.Ok(c.Id, $"Customer {c.FullName} created.");
    }

    public async Task<Result> UpdateAsync(UpdateCustomerCommand cmd)
    {
        var c = await _uow.Customers.GetByIdAsync(cmd.Id);
        if (c is null) return Result.Fail("Customer not found.");
        c.FirstName = cmd.FirstName;
        c.LastName  = cmd.LastName;
        c.Phone     = cmd.Phone;
        c.Notes     = cmd.Notes;
        c.Status    = cmd.Status;
        await _uow.Customers.UpdateAsync(c);
        await _uow.SaveChangesAsync();
        return Result.Ok($"Customer {c.FullName} updated.");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var c = await _uow.Customers.GetByIdAsync(id);
        if (c is null) return Result.Fail("Customer not found.");
        await _uow.Customers.DeleteAsync(c);
        await _uow.SaveChangesAsync();
        return Result.Ok("Customer deleted.");
    }

    public async Task<Result> ToggleStatusAsync(int id)
    {
        var c = await _uow.Customers.GetByIdAsync(id);
        if (c is null) return Result.Fail("Customer not found.");
        c.Status = c.Status == CustomerStatus.Active
            ? CustomerStatus.Inactive
            : CustomerStatus.Active;
        await _uow.Customers.UpdateAsync(c);
        await _uow.SaveChangesAsync();
        return Result.Ok($"Customer {c.FullName} {c.Status.ToString().ToLower()}.");
    }
}
