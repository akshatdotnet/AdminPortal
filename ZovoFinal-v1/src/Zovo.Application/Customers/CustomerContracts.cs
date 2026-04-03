using System.ComponentModel.DataAnnotations;
using Zovo.Core.Enums;
using Zovo.Core.ValueObjects;

namespace Zovo.Application.Customers;

public record CustomerListItemDto(
    int Id, string FullName, string Email, string? Phone,
    string Status, int OrderCount, decimal TotalSpent, DateTime CreatedAt);

public record CustomerDetailDto(
    int Id, string FirstName, string LastName, string Email,
    string? Phone, string? AvatarUrl, string Status, string? Notes,
    int OrderCount, decimal TotalSpent, DateTime CreatedAt,
    IEnumerable<CustomerAddressDto> Addresses);

public record CustomerAddressDto(
    int Id, string Line1, string? Line2, string City,
    string State, string PostalCode, string Country, bool IsDefault);

public class CreateCustomerCommand
{
    [Required, StringLength(100)] public string FirstName { get; set; } = "";
    [Required, StringLength(100)] public string LastName  { get; set; } = "";
    [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = "";
    [Phone, StringLength(20)] public string? Phone { get; set; }
    [StringLength(1000)]      public string? Notes { get; set; }
}

public class UpdateCustomerCommand : CreateCustomerCommand
{
    public int Id { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
}

public class CustomerQueryParams
{
    public string? Search  { get; set; }
    public string? Status  { get; set; }
    public string  SortBy  { get; set; } = "newest";
    public int     Page    { get; set; } = 1;
    public int     PageSize { get; set; } = 20;
}

public interface ICustomerService
{
    Task<PagedResult<CustomerListItemDto>> GetPagedAsync(CustomerQueryParams q);
    Task<CustomerDetailDto?> GetDetailAsync(int id);
    Task<Result<int>> CreateAsync(CreateCustomerCommand cmd);
    Task<Result> UpdateAsync(UpdateCustomerCommand cmd);
    Task<Result> DeleteAsync(int id);
    Task<Result> ToggleStatusAsync(int id);
}
