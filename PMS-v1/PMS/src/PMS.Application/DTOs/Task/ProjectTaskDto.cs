using PMS.Domain.Enums;
using TaskStatus = PMS.Domain.Enums.TaskStatus;

namespace PMS.Application.DTOs.Task;

public class ProjectTaskDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public double TotalHoursLogged { get; set; }
    public bool HasActiveTimer { get; set; }
    public string StatusDisplay => Status.ToString();
    public string PriorityDisplay => Priority.ToString();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}