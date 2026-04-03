using PMS.Domain.Enums;
using TaskStatus = PMS.Domain.Enums.TaskStatus;

namespace PMS.Application.DTOs.Task;

public class UpdateTaskDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public int ProjectId { get; set; }
    public int? AssignedUserId { get; set; }
}