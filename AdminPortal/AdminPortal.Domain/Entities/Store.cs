namespace AdminPortal.Domain.Entities;

public class Store
{
    public Guid Id { get; set; }
    public string StoreLink { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string StoreAddress { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
