using PMS.Application.DTOs.Common;
using PMS.Domain.Entities;
using TaskStatus = PMS.Domain.Enums.TaskStatus;

namespace PMS.Application.Interfaces.Repositories;

public interface ITaskRepository : IGenericRepository<ProjectTask>
{
    /// <summary>EF Core — includes Project + AssignedUser + TimeLogs.</summary>
    Task<ProjectTask?> GetByIdWithDetailsAsync(int id);

    /// <summary>Dapper — fast paginated read with optional status filter.</summary>
    Task<PagedResultDto<ProjectTask>> GetPagedAsync(
        QueryParameters parameters,
        int? projectId = null,
        TaskStatus? statusFilter = null);

    /// <summary>All tasks for a given project (EF Core).</summary>
    Task<IEnumerable<ProjectTask>> GetByProjectIdAsync(int projectId);
}