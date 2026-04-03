using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Common;
using PMS.Application.DTOs.Task;
using PMS.Application.Interfaces.Services;
using PMS.Web.ViewModels.Task;
using TaskStatus = PMS.Domain.Enums.TaskStatus;

namespace PMS.Web.Controllers;

public class TaskController : Controller
{
    private readonly ITaskService _taskService;
    private readonly IProjectService _projectService;
    private readonly IUserService _userService;
    private readonly ILogger<TaskController> _logger;

    public TaskController(
        ITaskService taskService,
        IProjectService projectService,
        IUserService userService,
        ILogger<TaskController> logger)
    {
        _taskService = taskService;
        _projectService = projectService;
        _userService = userService;
        _logger = logger;
    }

    // ── GET /Task ─────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 10,
        string? search = null,
        string? sortBy = null,
        bool sortDesc = false,
        int? projectId = null,
        TaskStatus? statusFilter = null)
    {
        var query = new QueryParameters
        {
            PageNumber = page,
            PageSize = pageSize,
            SearchTerm = search,
            SortBy = sortBy,
            SortDesc = sortDesc
        };

        var pagedResult = await _taskService.GetPagedAsync(
            query, projectId, statusFilter);

        var vm = new TaskIndexViewModel
        {
            PagedResult = pagedResult,
            Query = query,
            Projects = await _projectService.GetAllActiveAsync(),
            Users = await _userService.GetAllActiveAsync(),
            FilterProjectId = projectId,
            FilterStatus = statusFilter
        };

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("_TaskList", vm);

        return View(vm);
    }

    // ── GET /Task/Detail/5 ────────────────────────────────────────────────────
    public async Task<IActionResult> Detail(int id)
    {
        var task = await _taskService.GetByIdAsync(id);
        if (task is null) return NotFound();

        return View(task);
    }

    // ── GET /Task/GetByProject/5 (used by Project Detail page) ───────────────
    [HttpGet]
    public async Task<IActionResult> GetByProject(int projectId)
    {
        var tasks = await _taskService.GetByProjectIdAsync(projectId);
        return PartialView("_TaskRow", tasks);
    }

    // ── GET /Task/GetForm?id=5&projectId=2 (AJAX) ─────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetForm(int? id = null, int? projectId = null)
    {
        var projects = await _projectService.GetAllActiveAsync();
        var users = await _userService.GetAllActiveAsync();

        ViewBag.Projects = projects;
        ViewBag.Users = users;

        if (id.HasValue)
        {
            var task = await _taskService.GetByIdAsync(id.Value);
            if (task is null) return NotFound();

            var updateDto = new UpdateTaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                DueDate = task.DueDate,
                ProjectId = task.ProjectId,
                AssignedUserId = task.AssignedUserId
            };

            return PartialView("_TaskForm", updateDto);
        }

        return PartialView("_TaskForm", new CreateTaskDto
        {
            ProjectId = projectId ?? 0,
            Status = TaskStatus.Pending
        });
    }

    // ── POST /Task/Create (AJAX) ──────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreateTaskDto dto)
    {
        try
        {
            var created = await _taskService.CreateAsync(dto);
            return Json(new
            {
                success = true,
                message = $"Task \"{created.Title}\" created successfully.",
                id = created.Id,
                projectId = created.ProjectId
            });
        }
        catch (FluentValidation.ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage));

            return Json(new { success = false, validationErrors = errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task");
            return Json(new
            {
                success = false,
                message = "An error occurred while creating the task."
            });
        }
    }

    // ── POST /Task/Update (AJAX) ──────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromForm] UpdateTaskDto dto)
    {
        try
        {
            var updated = await _taskService.UpdateAsync(dto);
            return Json(new
            {
                success = true,
                message = $"Task \"{updated.Title}\" updated successfully.",
                id = updated.Id,
                projectId = updated.ProjectId
            });
        }
        catch (FluentValidation.ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage));

            return Json(new { success = false, validationErrors = errors });
        }
        catch (KeyNotFoundException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task {TaskId}", dto.Id);
            return Json(new
            {
                success = false,
                message = "An error occurred while updating the task."
            });
        }
    }

    // ── POST /Task/Delete/5 (AJAX) ────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _taskService.DeleteAsync(id);
            return Json(new { success = true, message = "Task deleted successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task {TaskId}", id);
            return Json(new
            {
                success = false,
                message = "An error occurred while deleting the task."
            });
        }
    }

    // ── POST /Task/UpdateStatus (AJAX — quick status change) ─────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, TaskStatus status)
    {
        try
        {
            var task = await _taskService.GetByIdAsync(id);
            if (task is null)
                return Json(new { success = false, message = "Task not found." });

            var updateDto = new UpdateTaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = status,
                Priority = task.Priority,
                DueDate = task.DueDate,
                ProjectId = task.ProjectId,
                AssignedUserId = task.AssignedUserId
            };

            await _taskService.UpdateAsync(updateDto);
            return Json(new
            {
                success = true,
                message = $"Status updated to {status}.",
                status = status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task status {TaskId}", id);
            return Json(new { success = false, message = "Failed to update status." });
        }
    }
}