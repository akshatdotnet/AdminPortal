namespace AdminPortal.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public CustomerType Type { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSales { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastOrderAt { get; set; }
}

public enum CustomerType { New, Returning, Imported }
