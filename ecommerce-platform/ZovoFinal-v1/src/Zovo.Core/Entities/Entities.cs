using Zovo.Core.Enums;

namespace Zovo.Core.Entities;

public class Product : AuditableEntity
{
    public string Name             { get; set; } = string.Empty;
    public string? SKU             { get; set; }
    public string? Slug            { get; set; }
    public string Category         { get; set; } = string.Empty;
    public string? SubCategory     { get; set; }
    public decimal Price           { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal? CostPrice      { get; set; }
    public int Stock               { get; set; }
    public int LowStockThreshold   { get; set; } = 10;
    public bool IsActive           { get; set; } = true;
    public bool IsFeatured         { get; set; }
    public string? ImageUrl        { get; set; }
    public string? Description     { get; set; }
    public string? Tags            { get; set; }
    public decimal Weight          { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public class Customer : AuditableEntity
{
    public string FirstName        { get; set; } = string.Empty;
    public string LastName         { get; set; } = string.Empty;
    public string Email            { get; set; } = string.Empty;
    public string? Phone           { get; set; }
    public string? AvatarUrl       { get; set; }
    public CustomerStatus Status   { get; set; } = CustomerStatus.Active;
    public string? Notes           { get; set; }
    public ICollection<Order>   Orders    { get; set; } = new List<Order>();
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    // Computed (not mapped)
    public string FullName  => $"{FirstName} {LastName}";
    public string Initials  => FirstName.Length > 0 && LastName.Length > 0
        ? $"{FirstName[0]}{LastName[0]}".ToUpper() : "??";
}

public class Address : BaseEntity
{
    public int    CustomerId { get; set; }
    public string Line1      { get; set; } = string.Empty;
    public string? Line2     { get; set; }
    public string City       { get; set; } = string.Empty;
    public string State      { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country    { get; set; } = "India";
    public bool   IsDefault  { get; set; }
    public Customer? Customer { get; set; }
}

public class Order : AuditableEntity
{
    public string        OrderNumber       { get; set; } = string.Empty;
    public int           CustomerId        { get; set; }
    public int?          ShippingAddressId { get; set; }
    public OrderStatus   Status            { get; set; } = OrderStatus.Pending;
    public PaymentStatus PaymentStatus     { get; set; } = PaymentStatus.Pending;
    public decimal SubTotal               { get; set; }
    public decimal ShippingCost           { get; set; }
    public decimal Discount               { get; set; }
    public decimal TaxAmount              { get; set; }
    public decimal TotalAmount            { get; set; }
    public string? Notes                  { get; set; }
    public string? TrackingNumber         { get; set; }
    public string? CouponCode             { get; set; }
    public DateTime? ShippedAt            { get; set; }
    public DateTime? DeliveredAt          { get; set; }
    public Customer? Customer             { get; set; }
    public Address?  ShippingAddress      { get; set; }
    public ICollection<OrderItem> Items   { get; set; } = new List<OrderItem>();
}

public class OrderItem : BaseEntity
{
    public int     OrderId     { get; set; }
    public int     ProductId   { get; set; }
    public string  ProductName { get; set; } = string.Empty;
    public string? ProductSKU  { get; set; }
    public int     Quantity    { get; set; }
    public decimal UnitPrice   { get; set; }
    public decimal Discount    { get; set; }
    public decimal LineTotal   => (UnitPrice - Discount) * Quantity;
    public Order?  Order       { get; set; }
    public Product? Product    { get; set; }
}

public class StoreSettings : BaseEntity
{
    public string StoreName              { get; set; } = "Zovo Store";
    public string Currency               { get; set; } = "INR";
    public string CurrencySymbol         { get; set; } = "₹";
    public string TimeZone               { get; set; } = "Asia/Kolkata";
    public string? LogoUrl               { get; set; }
    public string? SupportEmail          { get; set; }
    public string? SupportPhone          { get; set; }
    public decimal TaxRate               { get; set; } = 18m;
    public decimal FreeShippingThreshold { get; set; } = 500m;
    public decimal DefaultShippingCost   { get; set; } = 49m;
}
