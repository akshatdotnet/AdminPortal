using PMS.Application.DTOs.Common;
using PMS.Application.DTOs.Project;
using PMS.Domain.Enums;

namespace PMS.Web.ViewModels.Project;

public class ProjectIndexViewModel
{
    public PagedResultDto<ProjectDto> PagedResult { get; set; } = new();
    public QueryParameters Query { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}