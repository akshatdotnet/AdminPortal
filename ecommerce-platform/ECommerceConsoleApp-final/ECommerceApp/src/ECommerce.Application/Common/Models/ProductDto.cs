namespace ECommerce.Application.Common.Models;

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int AvailableStock,
    Guid CategoryId,
    bool IsActive
);
