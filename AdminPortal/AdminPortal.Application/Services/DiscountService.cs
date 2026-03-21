using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;
using AdminPortal.Application.Interfaces;
using AdminPortal.Domain.Entities;
using AdminPortal.Domain.Interfaces;

namespace AdminPortal.Application.Services;

public class DiscountService : IDiscountService
{
    private readonly IDiscountRepository _discountRepository;

    public DiscountService(IDiscountRepository discountRepository)
    {
        _discountRepository = discountRepository;
    }

    public async Task<Result<IEnumerable<DiscountDto>>> GetAllDiscountsAsync()
    {
        var discounts = await _discountRepository.GetAllAsync();
        return Result<IEnumerable<DiscountDto>>.Success(discounts.Select(MapToDto));
    }

    public async Task<Result<DiscountDto>> GetDiscountByIdAsync(Guid id)
    {
        var discount = await _discountRepository.GetByIdAsync(id);
        return discount is null
            ? Result<DiscountDto>.Failure("Discount not found.")
            : Result<DiscountDto>.Success(MapToDto(discount));
    }

    public async Task<Result<DiscountDto>> CreateDiscountAsync(CreateDiscountDto dto)
    {
        var existing = await _discountRepository.GetByCodeAsync(dto.Code);
        if (existing is not null)
            return Result<DiscountDto>.Failure("A discount with this code already exists.");

        var discount = new Discount
        {
            Id = Guid.NewGuid(),
            Code = dto.Code.ToUpper(),
            Description = dto.Description,
            Type = dto.Type,
            Value = dto.Value,
            UsageLimit = dto.UsageLimit,
            ExpiresAt = dto.ExpiresAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _discountRepository.AddAsync(discount);
        return Result<DiscountDto>.Success(MapToDto(created));
    }

    public async Task<Result> DeleteDiscountAsync(Guid id)
    {
        var deleted = await _discountRepository.DeleteAsync(id);
        return deleted ? Result.Success() : Result.Failure("Discount not found.");
    }

    public async Task<Result<DiscountDto>> ToggleDiscountAsync(Guid id)
    {
        var discount = await _discountRepository.GetByIdAsync(id);
        if (discount is null)
            return Result<DiscountDto>.Failure("Discount not found.");

        discount.IsActive = !discount.IsActive;
        var updated = await _discountRepository.UpdateAsync(discount);
        return Result<DiscountDto>.Success(MapToDto(updated));
    }

    private static DiscountDto MapToDto(Discount d) => new()
    {
        Id = d.Id,
        Code = d.Code,
        Description = d.Description,
        Type = d.Type.ToString(),
        Value = d.Value,
        DisplayValue = d.Type == DiscountType.Percentage ? $"{d.Value}% OFF" : $"\u20B9{d.Value} OFF",
        UsageLimit = d.UsageLimit,
        UsedCount = d.UsedCount,
        ExpiresAt = d.ExpiresAt,
        IsActive = d.IsActive
    };
}
