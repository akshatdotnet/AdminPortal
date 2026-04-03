namespace PMS.Application.DTOs.TimeLog;

public class TaskTimeLogDto
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double? TotalHours { get; set; }
    public string FormattedDuration { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsRunning { get; set; }
    public DateTime CreatedAt { get; set; }
}