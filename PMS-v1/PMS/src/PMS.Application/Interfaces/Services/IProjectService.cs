using PMS.Application.DTOs.Common;
using PMS.Application.DTOs.Project;

namespace PMS.Application.Interfaces.Services;

public interface IProjectService
{
    Task<PagedResultDto<ProjectDto>> GetPagedAsync(QueryParameters parameters);
    Task<ProjectDto?> GetByIdAsync(int id);
    Task<ProjectDto> CreateAsync(CreateProjectDto dto);
    Task<ProjectDto> UpdateAsync(UpdateProjectDto dto);
    Task DeleteAsync(int id);
    Task<IEnumerable<ProjectDto>> GetAllActiveAsync();
}