// AdminPortal.Infrastructure/ApiClient/AdminApiClient.cs
// Drop this file into your existing Infrastructure project and register it in DI.
// It replaces the mock repositories when you switch to the real API.

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdminPortal.Infrastructure.ApiClient;

// ── Shared response wrappers (mirror the API's DTOs) ─────────────────────────

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}

public class PagedResponse<T>
{
    public bool Success { get; set; }
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

// ── Typed HTTP client ─────────────────────────────────────────────────────────

public class AdminApiClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AdminApiClient(HttpClient http) => _http = http;

    // ── Dashboard ─────────────────────────────────────────────────────────────

    public async Task<DashboardApiDto?> GetDashboardAsync()
    {
        var resp = await _http.GetFromJsonAsync<ApiResponse<DashboardApiDto>>("/api/dashboard", _json);
        return resp?.Data;
    }

    // ── Products ──────────────────────────────────────────────────────────────

    public async Task<PagedResponse<ProductApiDto>> GetProductsAsync(
        string? search = null, string? category = null,
        bool? isActive = null, int page = 1, int pageSize = 10)
    {
        var url = BuildUrl("/api/products", ("search", search), ("category", category),
            ("isActive", isActive?.ToString()), ("page", page.ToString()), ("pageSize", pageSize.ToString()));

        return await _http.GetFromJsonAsync<PagedResponse<ProductApiDto>>(url, _json)
               ?? new PagedResponse<ProductApiDto>();
    }

    public async Task<ProductApiDto?> GetProductAsync(int id)
    {
        var resp = await _http.GetFromJsonAsync<ApiResponse<ProductApiDto>>($"/api/products/{id}", _json);
        return resp?.Data;
    }

    public async Task<ProductApiDto?> ToggleProductAsync(int id)
    {
        var resp = await _http.PatchAsync($"/api/products/{id}/toggle", null);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ApiResponse<ProductApiDto>>(_json);
        return result?.Data;
    }

    public async Task<IEnumerable<string>> GetProductCategoriesAsync()
    {
        var resp = await _http.GetFromJsonAsync<ApiResponse<IEnumerable<string>>>("/api/products/categories", _json);
        return resp?.Data ?? Enumerable.Empty<string>();
    }

    // ── Orders ────────────────────────────────────────────────────────────────

    public async Task<PagedResponse<OrderApiDto>> GetOrdersAsync(
        string? status = null, string? search = null, int page = 1, int pageSize = 10)
    {
        var url = BuildUrl("/api/orders", ("status", status), ("search", search),
            ("page", page.ToString()), ("pageSize", pageSize.ToString()));

        return await _http.GetFromJsonAsync<PagedResponse<OrderApiDto>>(url, _json)
               ?? new PagedResponse<OrderApiDto>();
    }

    public async Task<OrderApiDto?> GetOrderAsync(int id)
    {
        var resp = await _http.GetFromJsonAsync<ApiResponse<OrderApiDto>>($"/api/orders/{id}", _json);
        return resp?.Data;
    }

    public async Task<OrderApiDto?> UpdateOrderStatusAsync(int id, string status)
    {
        var resp = await _http.PatchAsJsonAsync($"/api/orders/{id}/status", new { status }, _json);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ApiResponse<OrderApiDto>>(_json);
        return result?.Data;
    }

    // ── Discounts ─────────────────────────────────────────────────────────────

    public async Task<IEnumerable<DiscountApiDto>> GetDiscountsAsync(bool? isActive = null)
    {
        var url = BuildUrl("/api/discounts", ("isActive", isActive?.ToString()));
        var resp = await _http.GetFromJsonAsync<ApiResponse<IEnumerable<DiscountApiDto>>>(url, _json);
        return resp?.Data ?? Enumerable.Empty<DiscountApiDto>();
    }

    public async Task<DiscountApiDto?> CreateDiscountAsync(CreateDiscountApiRequest request)
    {
        var resp = await _http.PostAsJsonAsync("/api/discounts", request, _json);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ApiResponse<DiscountApiDto>>(_json);
        return result?.Data;
    }

    public async Task<DiscountApiDto?> ToggleDiscountAsync(int id)
    {
        var resp = await _http.PatchAsync($"/api/discounts/{id}/toggle", null);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ApiResponse<DiscountApiDto>>(_json);
        return result?.Data;
    }

    public async Task DeleteDiscountAsync(int id)
    {
        var resp = await _http.DeleteAsync($"/api/discounts/{id}");
        resp.EnsureSuccessStatusCode();
    }

    // ── Payouts ───────────────────────────────────────────────────────────────

    public async Task<PayoutsOverviewApiDto?> GetPayoutsOverviewAsync()
    {
        var resp = await _http.GetFromJsonAsync<ApiResponse<PayoutsOverviewApiDto>>("/api/payouts", _json);
        return resp?.Data;
    }

    public async Task<PayoutApiDto?> RequestPayoutAsync(decimal amount)
    {
        var resp = await _http.PostAsJsonAsync("/api/payouts/request", new { amount }, _json);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ApiResponse<PayoutApiDto>>(_json);
        return result?.Data;
    }

    // ── Settings: Store ───────────────────────────────────────────────────────

    public async Task<StoreApiDto?> GetStoreAsync()
    {
        var resp = await _http.GetFromJsonAsync<ApiResponse<StoreApiDto>>("/api/settings/store", _json);
        return resp?.Data;
    }

    public async Task<StoreApiDto?> UpdateStoreAsync(UpdateStoreApiRequest request)
    {
        var resp = await _http.PutAsJsonAsync("/api/settings/store", request, _json);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ApiResponse<StoreApiDto>>(_json);
        return result?.Data;
    }

    public async Task<StoreApiDto?> ToggleStoreAsync()
    {
        var resp = await _http.PatchAsync("/api/settings/store/toggle", null);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ApiResponse<StoreApiDto>>(_json);
        return result?.Data;
    }

    // ── Settings: Staff ───────────────────────────────────────────────────────

    public async Task<IEnumerable<StaffApiDto>> GetStaffAsync()
    {
        var resp = await _http.GetFromJsonAsync<ApiResponse<IEnumerable<StaffApiDto>>>("/api/settings/staff", _json);
        return resp?.Data ?? Enumerable.Empty<StaffApiDto>();
    }

    public async Task<StaffApiDto?> AddStaffAsync(AddStaffApiRequest request)
    {
        var resp = await _http.PostAsJsonAsync("/api/settings/staff", request, _json);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ApiResponse<StaffApiDto>>(_json);
        return result?.Data;
    }

    public async Task<StaffApiDto?> ToggleStaffAsync(int id)
    {
        var resp = await _http.PatchAsync($"/api/settings/staff/{id}/toggle", null);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<ApiResponse<StaffApiDto>>(_json);
        return result?.Data;
    }

    public async Task DeleteStaffAsync(int id)
    {
        var resp = await _http.DeleteAsync($"/api/settings/staff/{id}");
        resp.EnsureSuccessStatusCode();
    }

    // ── Audience ──────────────────────────────────────────────────────────────

    public async Task<PagedResponse<AudienceApiDto>> GetAudienceAsync(
        string? tag = null, string? search = null, int page = 1, int pageSize = 10)
    {
        var url = BuildUrl("/api/audience", ("tag", tag), ("search", search),
            ("page", page.ToString()), ("pageSize", pageSize.ToString()));

        return await _http.GetFromJsonAsync<PagedResponse<AudienceApiDto>>(url, _json)
               ?? new PagedResponse<AudienceApiDto>();
    }

    // ── Credits ───────────────────────────────────────────────────────────────

    public async Task<CreditsOverviewApiDto?> GetCreditsOverviewAsync()
    {
        var resp = await _http.GetFromJsonAsync<ApiResponse<CreditsOverviewApiDto>>("/api/credits", _json);
        return resp?.Data;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildUrl(string path, params (string Key, string? Value)[] queryParams)
    {
        var qs = string.Join("&", queryParams
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}"));

        return string.IsNullOrEmpty(qs) ? path : $"{path}?{qs}";
    }
}

// ── API-side DTOs (consumed by the MVC project) ───────────────────────────────

public class DashboardApiDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int TotalProducts { get; set; }
    public int TotalCustomers { get; set; }
    public decimal RevenueChangePercent { get; set; }
    public int OrdersChangePercent { get; set; }
    public List<RevenuePointApiDto> RevenueChart { get; set; } = new();
    public List<ProductApiDto> TopProducts { get; set; } = new();
    public List<OrderApiDto> RecentOrders { get; set; } = new();
}

public class RevenuePointApiDto { public string Label { get; set; } = ""; public decimal Revenue { get; set; } }

public class ProductApiDto
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

public class OrderItemApiDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class OrderApiDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public DateTime OrderDate { get; set; }
    public List<OrderItemApiDto> Items { get; set; } = new();
}

public class DiscountApiDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Percentage { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public int UsageCount { get; set; }
    public int UsageLimit { get; set; }
    public bool IsExpired { get; set; }
}

public record CreateDiscountApiRequest(string Code, string Description, decimal Percentage, DateTime ExpiryDate, int UsageLimit);

public class PayoutApiDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string BankAccount { get; set; } = "";
    public string Reference { get; set; } = "";
}

public class PayoutsOverviewApiDto
{
    public decimal AvailableBalance { get; set; }
    public decimal TotalPaidOut { get; set; }
    public decimal PendingAmount { get; set; }
    public List<PayoutApiDto> History { get; set; } = new();
}

public class StoreApiDto
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

public record UpdateStoreApiRequest(string Name, string Description, string Category, string PhoneNumber, string Address);

public class StaffApiDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
}

public record AddStaffApiRequest(string Name, string Email, string Role);

public class AudienceApiDto
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

public class CreditDto { public int Id { get; set; } public decimal Amount { get; set; } public string Source { get; set; } = ""; public string Description { get; set; } = ""; public DateTime EarnedAt { get; set; } public bool IsUsed { get; set; } }

public class CreditsOverviewApiDto
{
    public decimal TotalCredits { get; set; }
    public decimal AvailableCredits { get; set; }
    public decimal UsedCredits { get; set; }
    public List<CreditDto> History { get; set; } = new();
}
