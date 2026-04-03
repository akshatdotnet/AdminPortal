using PMS.Domain.Common;
using PMS.Domain.Constants;
using System.Numerics;
using static PMS.Domain.Constants.DomainConstants;

namespace PMS.Domain.Entities;

public class TaskTimeLog : BaseEntity
{
    public int TaskId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Notes { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public ProjectTask Task { get; set; } = null!;

    // ── Computed ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Total hours for this log entry. Null if the timer is still running.
    /// </summary>
    public double? TotalHours =>
        EndTime.HasValue
            ? Math.Round((EndTime.Value - StartTime).TotalHours, 4)
            : null;

    /// <summary>Human-readable duration string, e.g. "1h 23m"</summary>
    public string FormattedDuration
    {
        get
        {
            if (!EndTime.HasValue)
                return "Running...";

            var duration = EndTime.Value - StartTime;

            if (duration.TotalMinutes < 1)
                return $"{duration.Seconds}s";

            if (duration.TotalHours < 1)
                return $"{duration.Minutes}m {duration.Seconds}s";

            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        }
    }

    /// <summary>Stops the timer and captures the EndTime.</summary>
    public void Stop()
    {
        if (EndTime.HasValue)
            throw new InvalidOperationException("Timer has already been stopped.");

        EndTime = DateTime.UtcNow;
    }

    

    /// <summary>Returns true if this log entry is still running.</summary>
    public bool IsRunning => !EndTime.HasValue;

    public int? UserId { get; set; }
}
