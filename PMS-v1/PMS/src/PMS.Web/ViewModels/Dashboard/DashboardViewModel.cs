using PMS.Application.DTOs.Project;
using PMS.Application.DTOs.Task;

namespace PMS.Web.ViewModels.Dashboard;

public class DashboardViewModel
{
    // ── Summary Stats ─────────────────────────────────────────────────────────
    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; }
    public int TotalTasks { get; set; }
    public int PendingTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverdueTasks { get; set; }
    public double TotalHoursLogged { get; set; }

    // ── Recent Data ───────────────────────────────────────────────────────────
    public IEnumerable<ProjectDto> RecentProjects { get; set; }
        = Enumerable.Empty<ProjectDto>();
    public IEnumerable<ProjectTaskDto> RecentTasks { get; set; }
        = Enumerable.Empty<ProjectTaskDto>();
    public IEnumerable<ProjectTaskDto> OverdueTaskList { get; set; }
        = Enumerable.Empty<ProjectTaskDto>();

    // ── Computed ──────────────────────────────────────────────────────────────
    public double TaskCompletionRate =>
        TotalTasks == 0 ? 0 :
        Math.Round((double)CompletedTasks / TotalTasks * 100, 1);
}