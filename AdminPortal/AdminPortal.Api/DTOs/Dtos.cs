namespace AdminPortal.Api.DTOs;

// ── Generic wrappers ──────────────────────────────────────────────────────────

public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public T? Data { get; set; }
    public string? Message { get; set; }
}

public class PagedResponse<T>
{
    public bool Success { get; set; } = true;
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

// ── Store ─────────────────────────────────────────────────────────────────────

public class StoreDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Address { get; set; } = "";
    public bool IsOpen { get; set; }
    public string LogoUrl { get; set; } = "";
}

// ── Product ───────────────────────────────────────────────────────────────────

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Category { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Order ─────────────────────────────────────────────────────────────────────

public class OrderItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

// ── Discount ──────────────────────────────────────────────────────────────────

public class DiscountDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Percentage { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public int UsageCount { get; set; }
    public int UsageLimit { get; set; }
    public bool IsExpired => ExpiryDate < DateTime.UtcNow;
}

// ── Payout ────────────────────────────────────────────────────────────────────

public class PayoutDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string BankAccount { get; set; } = "";
    public string Reference { get; set; } = "";
}

public class PayoutsOverviewDto
{
    public decimal AvailableBalance { get; set; }
    public decimal TotalPaidOut { get; set; }
    public decimal PendingAmount { get; set; }
    public List<PayoutDto> History { get; set; } = new();
}

// ── Staff ─────────────────────────────────────────────────────────────────────

public class StaffDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
}

// ── Audience ──────────────────────────────────────────────────────────────────

public class AudienceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime LastOrderDate { get; set; }
    public string Tag { get; set; } = "";
}

// ── Credits ───────────────────────────────────────────────────────────────────

public class CreditDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Source { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime EarnedAt { get; set; }
    public bool IsUsed { get; set; }
}

public class CreditsOverviewDto
{
    public decimal TotalCredits { get; set; }
    public decimal AvailableCredits { get; set; }
    public decimal UsedCredits { get; set; }
    public List<CreditDto> History { get; set; } = new();
}

// ── Analytics / Dashboard ─────────────────────────────────────────────────────

public class DashboardDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int TotalProducts { get; set; }
    public int TotalCustomers { get; set; }
    public decimal RevenueChangePercent { get; set; }
    public int OrdersChangePercent { get; set; }
    public List<RevenuePointDto> RevenueChart { get; set; } = new();
    public List<ProductDto> TopProducts { get; set; } = new();
    public List<OrderDto> RecentOrders { get; set; } = new();
}

public class RevenuePointDto
{
    public string Label { get; set; } = "";
    public decimal Revenue { get; set; }
}
