using AdminPortal.Domain.Entities;

namespace AdminPortal.Application.DTOs;

public class CustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public CustomerType TypeEnum { get; set; }
    public int TotalOrders { get; set; }
    public string TotalSales { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastOrderAt { get; set; }
    public string Initials => string.Concat(Name.Split(' ').Take(2).Select(n => n.FirstOrDefault())).ToUpper();
}

public class CreateCustomerDto
{
    public string Name { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}
