using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;
using AdminPortal.Application.Interfaces;
using AdminPortal.Domain.Interfaces;

namespace AdminPortal.Application.Services;

public class StoreService : IStoreService
{
    private readonly IStoreRepository _storeRepository;

    public StoreService(IStoreRepository storeRepository)
    {
        _storeRepository = storeRepository;
    }

    public async Task<Result<StoreDto>> GetCurrentStoreAsync()
    {
        var store = await _storeRepository.GetCurrentStoreAsync();
        if (store is null)
            return Result<StoreDto>.Failure("Store not found.");

        return Result<StoreDto>.Success(new StoreDto
        {
            Id = store.Id,
            StoreLink = store.StoreLink,
            StoreName = store.StoreName,
            MobileNumber = store.MobileNumber,
            EmailAddress = store.EmailAddress,
            Country = store.Country,
            Currency = store.Currency,
            StoreAddress = store.StoreAddress,
            IsOpen = store.IsOpen
        });
    }

    public async Task<Result<StoreDto>> UpdateStoreAsync(UpdateStoreDto dto)
    {
        var store = await _storeRepository.GetCurrentStoreAsync();
        if (store is null)
            return Result<StoreDto>.Failure("Store not found.");

        store.StoreName = dto.StoreName;
        store.MobileNumber = dto.MobileNumber;
        store.EmailAddress = dto.EmailAddress;
        store.StoreAddress = dto.StoreAddress;
        store.IsOpen = dto.IsOpen;
        store.UpdatedAt = DateTime.UtcNow;

        var updated = await _storeRepository.UpdateAsync(store);
        return Result<StoreDto>.Success(new StoreDto
        {
            Id = updated.Id,
            StoreLink = updated.StoreLink,
            StoreName = updated.StoreName,
            MobileNumber = updated.MobileNumber,
            EmailAddress = updated.EmailAddress,
            Country = updated.Country,
            Currency = updated.Currency,
            StoreAddress = updated.StoreAddress,
            IsOpen = updated.IsOpen
        });
    }

    public async Task<Result<StoreDto>> ToggleStoreStatusAsync(Guid id)
    {
        var store = await _storeRepository.GetByIdAsync(id);
        if (store is null)
            return Result<StoreDto>.Failure("Store not found.");

        var updated = await _storeRepository.UpdateStoreStatusAsync(id, !store.IsOpen);
        return Result<StoreDto>.Success(new StoreDto
        {
            Id = updated.Id,
            StoreName = updated.StoreName,
            IsOpen = updated.IsOpen
        });
    }
}
