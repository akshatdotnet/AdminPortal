using PMS.Domain.Common;
using PMS.Domain.Constants;
using PMS.Domain.Enums;

namespace PMS.Domain.Entities;

public class ProjectTask : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Enums.TaskStatus Status { get; set; } = Enums.TaskStatus.Pending;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateTime? DueDate { get; set; }

    public string Phase { get; set; } = "Pending"; // default

    // ── Foreign Keys ─────────────────────────────────────────────────────────
    public int ProjectId { get; set; }
    public int? AssignedUserId { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public Project Project { get; set; } = null!;
    public User? AssignedUser { get; set; }
    public ICollection<TaskTimeLog> TimeLogs { get; set; } = new List<TaskTimeLog>();

    // ── Computed ──────────────────────────────────────────────────────────────

    /// <summary>Total hours tracked across all completed time logs for this task.</summary>
    public double TotalHoursLogged =>
        Math.Round(TimeLogs
            .Where(t => t.EndTime.HasValue)
            .Sum(t => t.TotalHours ?? 0), 2);

    /// <summary>Returns true if the task currently has an active (running) timer.</summary>
    public bool HasActiveTimer =>
        TimeLogs.Any(t => t.StartTime != default && !t.EndTime.HasValue);
}