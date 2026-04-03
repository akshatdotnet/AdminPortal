using PMS.Application.DTOs.TimeLog;

namespace PMS.Application.Interfaces.Services;

public interface ITimeLogService
{
    Task<TaskTimeLogDto> StartTimerAsync(int taskId, string? notes = null);
    Task<TaskTimeLogDto> StopTimerAsync(int taskId);
    Task<TimeLogSummaryDto> GetSummaryAsync(int taskId);
    Task<IEnumerable<TaskTimeLogDto>> GetLogsByTaskIdAsync(int taskId);
    Task DeleteLogAsync(int logId);
}