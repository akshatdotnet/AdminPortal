using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;

namespace AdminPortal.Application.Interfaces;

public interface IStaffService
{
    Task<Result<IEnumerable<StaffDto>>> GetAllStaffAsync();
    Task<Result<StaffDto>> InviteStaffAsync(InviteStaffDto dto);
    Task<Result> RemoveStaffAsync(Guid id);
    Task<Result<StaffDto>> UpdateStaffRoleAsync(Guid id, string role);
}
