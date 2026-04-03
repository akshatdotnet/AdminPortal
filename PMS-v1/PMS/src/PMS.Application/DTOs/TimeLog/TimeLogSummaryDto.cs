namespace PMS.Application.DTOs.TimeLog;

public class TimeLogSummaryDto
{
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public double TotalHoursLogged { get; set; }
    public bool HasActiveTimer { get; set; }
    public int? ActiveLogId { get; set; }
    public IEnumerable<TaskTimeLogDto> Logs { get; set; }
        = Enumerable.Empty<TaskTimeLogDto>();
}