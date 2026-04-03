using PMS.Domain.Enums;

namespace PMS.Application.DTOs.Project;

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ProjectStatus Status { get; set; }
    public string StatusDisplay => Status.ToString();
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public double CompletionPercentage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}