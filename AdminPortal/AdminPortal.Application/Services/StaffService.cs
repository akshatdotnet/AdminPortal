using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;
using AdminPortal.Application.Interfaces;
using AdminPortal.Domain.Entities;
using AdminPortal.Domain.Interfaces;

namespace AdminPortal.Application.Services;

public class StaffService : IStaffService
{
    private readonly IStaffRepository _staffRepository;

    public StaffService(IStaffRepository staffRepository)
    {
        _staffRepository = staffRepository;
    }

    public async Task<Result<IEnumerable<StaffDto>>> GetAllStaffAsync()
    {
        var staff = await _staffRepository.GetAllAsync();
        return Result<IEnumerable<StaffDto>>.Success(staff.Select(MapToDto));
    }

    public async Task<Result<StaffDto>> InviteStaffAsync(InviteStaffDto dto)
    {
        var existing = await _staffRepository.GetByEmailAsync(dto.Email);
        if (existing is not null)
            return Result<StaffDto>.Failure("A staff member with this email already exists.");

        var staff = new StaffAccount
        {
            Id = Guid.NewGuid(),
            Name = dto.Email.Split('@')[0],
            Email = dto.Email,
            Role = dto.Role,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };

        var created = await _staffRepository.AddAsync(staff);
        return Result<StaffDto>.Success(MapToDto(created));
    }

    public async Task<Result> RemoveStaffAsync(Guid id)
    {
        var deleted = await _staffRepository.DeleteAsync(id);
        return deleted ? Result.Success() : Result.Failure("Staff member not found.");
    }

    public async Task<Result<StaffDto>> UpdateStaffRoleAsync(Guid id, string role)
    {
        var staff = await _staffRepository.GetByIdAsync(id);
        if (staff is null)
            return Result<StaffDto>.Failure("Staff member not found.");

        staff.Role = role;
        var updated = await _staffRepository.UpdateAsync(staff);
        return Result<StaffDto>.Success(MapToDto(updated));
    }

    private static StaffDto MapToDto(StaffAccount s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Email = s.Email,
        Role = s.Role,
        IsActive = s.IsActive,
        JoinedAt = s.JoinedAt,
        LastLoginAt = s.LastLoginAt
    };
}
