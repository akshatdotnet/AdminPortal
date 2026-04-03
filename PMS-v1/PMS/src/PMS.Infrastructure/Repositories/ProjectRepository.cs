using Dapper;
using Microsoft.EntityFrameworkCore;
using PMS.Application.DTOs.Common;
using PMS.Application.Interfaces.Repositories;
using PMS.Domain.Entities;
using PMS.Infrastructure.Dapper;
using PMS.Infrastructure.Data;

namespace PMS.Infrastructure.Repositories;

public class ProjectRepository : GenericRepository<Project>, IProjectRepository
{
    private readonly DapperContext _dapper;

    public ProjectRepository(ApplicationDbContext context, DapperContext dapper)
        : base(context)
    {
        _dapper = dapper;
    }

    public async Task<Project?> GetByIdWithTasksAsync(int id)
        => await _context.Projects
            .AsNoTracking()                      // read-only — no change tracking
            .AsSplitQuery()                      // avoids Cartesian explosion
            .Include(p => p.Tasks
                .Where(t => !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt))
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<PagedResultDto<Project>> GetPagedAsync(
        QueryParameters parameters)
    {
        var offset = (parameters.PageNumber - 1) * parameters.PageSize;
        var searchTerm = string.IsNullOrWhiteSpace(parameters.SearchTerm)
            ? null
            : $"%{parameters.SearchTerm.Trim()}%";

        var sortColumn = parameters.SortBy?.ToLower() switch
        {
            "name" => "Name",
            "status" => "Status",
            "startdate" => "StartDate",
            "enddate" => "EndDate",
            "createdat" => "CreatedAt",
            _ => "CreatedAt"
        };

        var sortDir = parameters.SortDesc ? "DESC" : "ASC";

        // Single round-trip with multiple result sets
        var sql = $"""
            SELECT
                Id, Name, Description, StartDate, EndDate,
                Status, CreatedAt, UpdatedAt, IsDeleted
            FROM Projects
            WHERE IsDeleted = 0
              AND (@Search IS NULL
                   OR Name        LIKE @Search
                   OR Description LIKE @Search)
            ORDER BY {sortColumn} {sortDir}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(1)
            FROM Projects
            WHERE IsDeleted = 0
              AND (@Search IS NULL
                   OR Name        LIKE @Search
                   OR Description LIKE @Search);
            """;

        using var connection = _dapper.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(
            sql,
            new { Search = searchTerm, Offset = offset, PageSize = parameters.PageSize },
            commandTimeout: 30);

        var items = (await multi.ReadAsync<Project>()).ToList();
        var total = await multi.ReadFirstAsync<int>();

        return new PagedResultDto<Project>
        {
            Items = items,
            TotalCount = total,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize
        };
    }
}


















//using Dapper;
//using Microsoft.EntityFrameworkCore;
//using PMS.Application.DTOs.Common;
//using PMS.Application.Interfaces.Repositories;
//using PMS.Domain.Entities;
//using PMS.Infrastructure.Dapper;
//using PMS.Infrastructure.Data;

//namespace PMS.Infrastructure.Repositories;

//public class ProjectRepository : GenericRepository<Project>, IProjectRepository
//{
//    private readonly DapperContext _dapper;

//    public ProjectRepository(ApplicationDbContext context, DapperContext dapper)
//        : base(context)
//    {
//        _dapper = dapper;
//    }

//    // EF Core — full object graph needed for computed properties
//    public async Task<Project?> GetByIdWithTasksAsync(int id)
//        => await _context.Projects
//            .Include(p => p.Tasks.Where(t => !t.IsDeleted))
//            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

//    // Dapper — fast paged read for listing page
//    public async Task<PagedResultDto<Project>> GetPagedAsync(QueryParameters parameters)
//    {
//        var offset = (parameters.PageNumber - 1) * parameters.PageSize;
//        var searchTerm = string.IsNullOrWhiteSpace(parameters.SearchTerm)
//            ? null
//            : $"%{parameters.SearchTerm.Trim()}%";

//        // Whitelist sortable columns to prevent SQL injection
//        var sortColumn = parameters.SortBy?.ToLower() switch
//        {
//            "name" => "Name",
//            "status" => "Status",
//            "startdate" => "StartDate",
//            "enddate" => "EndDate",
//            "createdat" => "CreatedAt",
//            _ => "CreatedAt"
//        };

//        var sortDir = parameters.SortDesc ? "DESC" : "ASC";

//        var sql = $"""
//            SELECT *
//            FROM   Projects
//            WHERE  IsDeleted = 0
//              AND  (@Search IS NULL OR Name LIKE @Search OR Description LIKE @Search)
//            ORDER BY {sortColumn} {sortDir}
//            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

//            SELECT COUNT(*)
//            FROM   Projects
//            WHERE  IsDeleted = 0
//              AND  (@Search IS NULL OR Name LIKE @Search OR Description LIKE @Search);
//            """;

//        using var connection = _dapper.CreateConnection();
//        using var multi = await connection.QueryMultipleAsync(sql, new
//        {
//            Search = searchTerm,
//            Offset = offset,
//            PageSize = parameters.PageSize
//        });

//        var items = (await multi.ReadAsync<Project>()).ToList();
//        var total = await multi.ReadFirstAsync<int>();

//        return new PagedResultDto<Project>
//        {
//            Items = items,
//            TotalCount = total,
//            PageNumber = parameters.PageNumber,
//            PageSize = parameters.PageSize
//        };
//    }
//}