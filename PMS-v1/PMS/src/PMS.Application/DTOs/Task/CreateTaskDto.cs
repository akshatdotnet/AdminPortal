using PMS.Domain.Enums;
using TaskStatus = PMS.Domain.Enums.TaskStatus;

namespace PMS.Application.DTOs.Task;

public class CreateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueDate { get; set; }
    public int ProjectId { get; set; }
    public int? AssignedUserId { get; set; }

    public string Phase { get; set; } = "Pending"; // default
}