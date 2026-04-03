using Microsoft.EntityFrameworkCore;
using PMS.Application.Interfaces.Repositories;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;

namespace PMS.Infrastructure.Repositories;

public class TimeLogRepository : GenericRepository<TaskTimeLog>, ITimeLogRepository
{
    public TimeLogRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<TaskTimeLog>> GetByTaskIdAsync(int taskId)
        => await _context.TimeLogs
            .Include(l => l.Task)
            .Where(l => l.TaskId == taskId && !l.IsDeleted)
            .OrderByDescending(l => l.StartTime)
            .ToListAsync();

    // Active = started but not yet stopped
    public async Task<TaskTimeLog?> GetActiveLogAsync(int taskId)
        => await _context.TimeLogs
            .Where(l => l.TaskId == taskId
                     && !l.IsDeleted
                     && l.EndTime == null)
            .FirstOrDefaultAsync();
}