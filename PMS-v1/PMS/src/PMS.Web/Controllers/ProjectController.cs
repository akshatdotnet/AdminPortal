using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Common;
using PMS.Application.DTOs.Project;
using PMS.Application.Interfaces.Services;
using PMS.Web.ViewModels.Project;

namespace PMS.Web.Controllers;

public class ProjectController : Controller
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectController> _logger;

    public ProjectController(
        IProjectService projectService,
        ILogger<ProjectController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    // ── GET /Project ──────────────────────────────────────────────────────────
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 10,
        string? search = null,
        string? sortBy = null,
        bool sortDesc = false)
    {
        var query = new QueryParameters
        {
            PageNumber = page,
            PageSize = pageSize,
            SearchTerm = search,
            SortBy = sortBy,
            SortDesc = sortDesc
        };

        var result = await _projectService.GetPagedAsync(query);

        var vm = new ProjectIndexViewModel
        {
            PagedResult = result,
            Query = query
        };

        // AJAX request → return partial only
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("_ProjectList", vm);

        return View(vm);
    }

    // ── GET /Project/Detail/5 ─────────────────────────────────────────────────
    public async Task<IActionResult> Detail(int id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project is null)
            return NotFound();

        return View(project);
    }

    // ── GET /Project/GetForm?id=5 (AJAX) ──────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetForm(int? id = null)
    {
        if (id.HasValue)
        {
            var project = await _projectService.GetByIdAsync(id.Value);
            if (project is null)
                return NotFound();

            var updateDto = new UpdateProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Status = project.Status
            };

            return PartialView("_ProjectForm", updateDto);
        }

        return PartialView("_ProjectForm", new CreateProjectDto
        {
            StartDate = DateTime.Today
        });
    }

    // ── POST /Project/Create (AJAX) ───────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreateProjectDto dto)
    {
        try
        {
            var created = await _projectService.CreateAsync(dto);
            return Json(new
            {
                success = true,
                message = $"Project \"{created.Name}\" created successfully.",
                id = created.Id
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
            _logger.LogError(ex, "Error creating project");
            return Json(new { success = false, message = "An error occurred while creating the project." });
        }
    }

    // ── POST /Project/Update (AJAX) ───────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromForm] UpdateProjectDto dto)
    {
        try
        {
            var updated = await _projectService.UpdateAsync(dto);
            return Json(new
            {
                success = true,
                message = $"Project \"{updated.Name}\" updated successfully.",
                id = updated.Id
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
            _logger.LogError(ex, "Error updating project {ProjectId}", dto.Id);
            return Json(new { success = false, message = "An error occurred while updating the project." });
        }
    }

    // ── POST /Project/Delete/5 (AJAX) ─────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _projectService.DeleteAsync(id);
            return Json(new { success = true, message = "Project deleted successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project {ProjectId}", id);
            return Json(new { success = false, message = "An error occurred while deleting the project." });
        }
    }
}