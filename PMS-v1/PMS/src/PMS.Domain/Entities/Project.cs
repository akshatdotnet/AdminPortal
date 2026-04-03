using PMS.Domain.Common;
using PMS.Domain.Constants;
using PMS.Domain.Enums;

namespace PMS.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.NotStarted;

    // ── Navigation ────────────────────────────────────────────────────────────
    public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();

    // ── Computed ──────────────────────────────────────────────────────────────
    public int TotalTasks => Tasks.Count(t => !t.IsDeleted);
    public int CompletedTasks => Tasks.Count(t => !t.IsDeleted
                                    && t.Status == Enums.TaskStatus.Completed);

    public double CompletionPercentage =>
       TotalTasks == 0 ? 0 : Math.Round((double)CompletedTasks / TotalTasks * 100, 1);
}