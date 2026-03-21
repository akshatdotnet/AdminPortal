using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;

namespace AdminPortal.Application.Interfaces;

public interface IStoreService
{
    Task<Result<StoreDto>> GetCurrentStoreAsync();
    Task<Result<StoreDto>> UpdateStoreAsync(UpdateStoreDto dto);
    Task<Result<StoreDto>> ToggleStoreStatusAsync(Guid id);
}
