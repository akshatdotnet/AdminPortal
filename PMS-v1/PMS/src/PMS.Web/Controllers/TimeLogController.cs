using Microsoft.AspNetCore.Mvc;
using PMS.Application.Interfaces.Services;
using PMS.Application.DTOs.TimeLog;

namespace PMS.Web.Controllers;

public class TimeLogController : Controller
{
    private readonly ITimeLogService _timeLogService;
    private readonly ILogger<TimeLogController> _logger;

    public TimeLogController(
        ITimeLogService timeLogService,
        ILogger<TimeLogController> logger)
    {
        _timeLogService = timeLogService;
        _logger = logger;
    }

    // ── GET /TimeLog/Summary/5 (AJAX — partial) ───────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Summary(int taskId)
    {
        try
        {
            var summary = await _timeLogService.GetSummaryAsync(taskId);
            return PartialView("_TimeLogSummary", summary);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading time log summary for task {TaskId}", taskId);
            return StatusCode(500, "Failed to load time logs.");
        }
    }

    // ── POST /TimeLog/Start (AJAX) ────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start([FromForm] int taskId,
                                           [FromForm] string? notes = null)
    {
        try
        {
            var log = await _timeLogService.StartTimerAsync(taskId, notes);

            return Json(new
            {
                success = true,
                message = "Timer started successfully.",
                logId = log.Id,
                taskId = log.TaskId,
                startTime = log.StartTime.ToString("o") // ISO 8601 for JS
            });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting timer for task {TaskId}", taskId);
            return Json(new { success = false, message = "Failed to start timer." });
        }
    }

    // ── POST /TimeLog/Stop (AJAX) ─────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Stop([FromForm] int taskId)
    {
        try
        {
            var log = await _timeLogService.StopTimerAsync(taskId);

            return Json(new
            {
                success = true,
                message = "Timer stopped successfully.",
                logId = log.Id,
                taskId = log.TaskId,
                formattedDuration = log.FormattedDuration,
                totalHours = log.TotalHours
            });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping timer for task {TaskId}", taskId);
            return Json(new { success = false, message = "Failed to stop timer." });
        }
    }

    // ── POST /TimeLog/Delete/5 (AJAX) ─────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromForm] int logId,
                                            [FromForm] int taskId)
    {
        try
        {
            await _timeLogService.DeleteLogAsync(logId);
            return Json(new { success = true, message = "Time log deleted." });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting log {LogId}", logId);
            return Json(new { success = false, message = "Failed to delete log." });
        }
    }

    // ── GET /TimeLog/GetLogs/5 (AJAX — JSON, for live refresh) ───────────────
    [HttpGet]
    public async Task<IActionResult> GetLogs(int taskId)
    {
        try
        {
            var summary = await _timeLogService.GetSummaryAsync(taskId);
            return Json(new
            {
                success = true,
                totalHoursLogged = summary.TotalHoursLogged,
                hasActiveTimer = summary.HasActiveTimer,
                activeLogId = summary.ActiveLogId,
                logs = summary.Logs.Select(l => new
                {
                    id = l.Id,
                    startTime = l.StartTime.ToString("o"),
                    endTime = l.EndTime?.ToString("o"),
                    totalHours = l.TotalHours,
                    formattedDuration = l.FormattedDuration,
                    notes = l.Notes,
                    isRunning = l.IsRunning
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching logs for task {TaskId}", taskId);
            return Json(new { success = false });
        }
    }
}