using PMS.Application.DTOs.Common;
using PMS.Application.DTOs.Project;
using PMS.Application.DTOs.Task;
using PMS.Domain.Entities;
using TaskStatus = PMS.Domain.Enums.TaskStatus;

namespace PMS.Web.ViewModels.Task;

public class TaskIndexViewModel
{
    public PagedResultDto<ProjectTaskDto> PagedResult { get; set; } = new();
    public QueryParameters Query { get; set; } = new();
    public IEnumerable<ProjectDto> Projects { get; set; } = Enumerable.Empty<ProjectDto>();
    public IEnumerable<User> Users { get; set; } = Enumerable.Empty<User>();
    public int? FilterProjectId { get; set; }
    public TaskStatus? FilterStatus { get; set; }
}