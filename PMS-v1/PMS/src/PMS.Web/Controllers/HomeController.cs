using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Common;
using PMS.Application.Interfaces.Services;
using PMS.Web.ViewModels.Dashboard;
using TaskStatus = PMS.Domain.Enums.TaskStatus;

namespace PMS.Web.Controllers;

public class HomeController : Controller
{
    private readonly IProjectService _projectService;
    private readonly ITaskService _taskService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IProjectService projectService,
        ITaskService taskService,
        ILogger<HomeController> logger)
    {
        _projectService = projectService;
        _taskService = taskService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        // ── Fetch data in parallel for performance ────────────────────────────
        var allProjectsTask = _projectService.GetPagedAsync(new QueryParameters
        {
            PageSize = 100
        });
        var allTasksTask = _taskService.GetPagedAsync(new QueryParameters
        {
            PageSize = 100
        });
        var recentProjectsTask = _projectService.GetPagedAsync(new QueryParameters
        {
            PageSize = 5,
            SortBy = "createdat",
            SortDesc = true
        });
        var recentTasksTask = _taskService.GetPagedAsync(new QueryParameters
        {
            PageSize = 8,
            SortBy = "createdat",
            SortDesc = true
        });

        await Task.WhenAll(
            allProjectsTask,
            allTasksTask,
            recentProjectsTask,
            recentTasksTask);

        var allProjects = await allProjectsTask;
        var allTasks = await allTasksTask;
        var recentProjects = await recentProjectsTask;
        var recentTasks = await recentTasksTask;

        var overdueTasks = allTasks.Items
            .Where(t => t.DueDate.HasValue
                     && t.DueDate.Value.Date < DateTime.Today
                     && t.Status != TaskStatus.Completed
                     && t.Status != TaskStatus.Cancelled)
            .ToList();

        var vm = new DashboardViewModel
        {
            TotalProjects = allProjects.TotalCount,
            ActiveProjects = allProjects.Items
                .Count(p => p.Status == Domain.Enums.ProjectStatus.Active),
            TotalTasks = allTasks.TotalCount,
            PendingTasks = allTasks.Items
                .Count(t => t.Status == TaskStatus.Pending),
            InProgressTasks = allTasks.Items
                .Count(t => t.Status == TaskStatus.InProgress),
            CompletedTasks = allTasks.Items
                .Count(t => t.Status == TaskStatus.Completed),
            OverdueTasks = overdueTasks.Count,
            TotalHoursLogged = allTasks.Items.Sum(t => t.TotalHoursLogged),
            RecentProjects = recentProjects.Items,
            RecentTasks = recentTasks.Items,
            OverdueTaskList = overdueTasks.Take(5)
        };

        return View(vm);
    }




    [Route("Home/NotFound")]
    public IActionResult NotFound404()
    {
        Response.StatusCode = 404;
        return View("NotFound");
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}