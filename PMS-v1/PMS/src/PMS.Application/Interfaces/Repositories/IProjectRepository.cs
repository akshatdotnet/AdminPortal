using PMS.Application.DTOs.Common;
using PMS.Domain.Entities;

namespace PMS.Application.Interfaces.Repositories;

public interface IProjectRepository : IGenericRepository<Project>
{
    /// <summary>EF Core — includes Tasks for computed properties.</summary>
    Task<Project?> GetByIdWithTasksAsync(int id);

    /// <summary>Dapper — fast paginated read for project listing page.</summary>
    Task<PagedResultDto<Project>> GetPagedAsync(QueryParameters parameters);
}