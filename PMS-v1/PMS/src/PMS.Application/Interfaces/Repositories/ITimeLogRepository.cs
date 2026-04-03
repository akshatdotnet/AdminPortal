using PMS.Domain.Entities;

namespace PMS.Application.Interfaces.Repositories;

public interface ITimeLogRepository : IGenericRepository<TaskTimeLog>
{
    /// <summary>All logs for a specific task, ordered newest first.</summary>
    Task<IEnumerable<TaskTimeLog>> GetByTaskIdAsync(int taskId);

    /// <summary>The currently running (no EndTime) log for a task, if any.</summary>
    Task<TaskTimeLog?> GetActiveLogAsync(int taskId);
}