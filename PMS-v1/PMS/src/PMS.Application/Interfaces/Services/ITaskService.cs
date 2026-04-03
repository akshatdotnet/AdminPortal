using PMS.Application.DTOs.Common;
using PMS.Application.DTOs.Task;
using TaskStatus = PMS.Domain.Enums.TaskStatus;

namespace PMS.Application.Interfaces.Services;

public interface ITaskService
{
    Task<PagedResultDto<ProjectTaskDto>> GetPagedAsync(
        QueryParameters parameters,
        int? projectId = null,
        TaskStatus? statusFilter = null);

    Task<ProjectTaskDto?> GetByIdAsync(int id);
    Task<ProjectTaskDto> CreateAsync(CreateTaskDto dto);
    Task<ProjectTaskDto> UpdateAsync(UpdateTaskDto dto);
    Task DeleteAsync(int id);
    Task<IEnumerable<ProjectTaskDto>> GetByProjectIdAsync(int projectId);
}