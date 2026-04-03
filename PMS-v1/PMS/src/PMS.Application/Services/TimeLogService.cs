using AutoMapper;
//using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.TimeLog;
using PMS.Application.Interfaces;
using PMS.Application.Interfaces.Services;
using PMS.Domain.Entities;

namespace PMS.Application.Services;

public class TimeLogService : ITimeLogService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    //private readonly ILogger<TimeLogService> _logger;

    public TimeLogService(
        IUnitOfWork uow,
        IMapper mapper)
        //,ILogger<TimeLogService> logger)
    {
        _uow = uow;
        _mapper = mapper;
       // _logger = logger;
    }

    public async Task<TaskTimeLogDto> StartTimerAsync(int taskId, string? notes = null)
    {
        // Ensure the task exists
        var taskExists = await _uow.Tasks.ExistsAsync(t => t.Id == taskId && !t.IsDeleted);
        if (!taskExists)
            throw new KeyNotFoundException($"Task {taskId} not found.");

        // Prevent double-starting
        var active = await _uow.TimeLogs.GetActiveLogAsync(taskId);
        if (active is not null)
            throw new InvalidOperationException(
                $"A timer is already running for task {taskId}. Stop it before starting a new one.");

        var log = new TaskTimeLog
        {
            TaskId = taskId,
            StartTime = DateTime.UtcNow,
            Notes = notes
        };

        await _uow.TimeLogs.AddAsync(log);
        await _uow.SaveChangesAsync();

        //_logger.LogInformation("Timer started. TaskId: {TaskId}, LogId: {LogId}",
        //    taskId, log.Id);

        return _mapper.Map<TaskTimeLogDto>(log);
    }

    public async Task<TaskTimeLogDto> StopTimerAsync(int taskId)
    {
        var active = await _uow.TimeLogs.GetActiveLogAsync(taskId)
            ?? throw new InvalidOperationException(
                $"No active timer found for task {taskId}.");

        active.Stop(); // domain method sets EndTime = UtcNow
        active.UpdatedAt = DateTime.UtcNow;

        _uow.TimeLogs.Update(active);
        await _uow.SaveChangesAsync();

        //_logger.LogInformation(
        //    "Timer stopped. TaskId: {TaskId}, LogId: {LogId}, Duration: {Duration}",
        //    taskId, active.Id, active.FormattedDuration);

        return _mapper.Map<TaskTimeLogDto>(active);
    }

    public async Task<TimeLogSummaryDto> GetSummaryAsync(int taskId)
    {
        var task = await _uow.Tasks.GetByIdWithDetailsAsync(taskId)
            ?? throw new KeyNotFoundException($"Task {taskId} not found.");

        var logs = await _uow.TimeLogs.GetByTaskIdAsync(taskId);
        var active = await _uow.TimeLogs.GetActiveLogAsync(taskId);

        return new TimeLogSummaryDto
        {
            TaskId = taskId,
            TaskTitle = task.Title,
            TotalHoursLogged = task.TotalHoursLogged,
            HasActiveTimer = active is not null,
            ActiveLogId = active?.Id,
            Logs = _mapper.Map<IEnumerable<TaskTimeLogDto>>(logs)
        };
    }

    public async Task<IEnumerable<TaskTimeLogDto>> GetLogsByTaskIdAsync(int taskId)
    {
        var logs = await _uow.TimeLogs.GetByTaskIdAsync(taskId);
        return _mapper.Map<IEnumerable<TaskTimeLogDto>>(logs);
    }

    public async Task DeleteLogAsync(int logId)
    {
        var log = await _uow.TimeLogs.GetByIdAsync(logId)
            ?? throw new KeyNotFoundException($"TimeLog {logId} not found.");

        if (log.IsRunning)
            throw new InvalidOperationException("Cannot delete a running timer. Stop it first.");

        log.SoftDelete();
        _uow.TimeLogs.Update(log);
        await _uow.SaveChangesAsync();

        //_logger.LogInformation("TimeLog deleted. LogId: {LogId}", logId);
    }
}