using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;

namespace AdminPortal.Application.Interfaces;

public interface IDiscountService
{
    Task<Result<IEnumerable<DiscountDto>>> GetAllDiscountsAsync();
    Task<Result<DiscountDto>> GetDiscountByIdAsync(Guid id);
    Task<Result<DiscountDto>> CreateDiscountAsync(CreateDiscountDto dto);
    Task<Result> DeleteDiscountAsync(Guid id);
    Task<Result<DiscountDto>> ToggleDiscountAsync(Guid id);
}
