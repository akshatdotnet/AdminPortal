using Dapper;
using Microsoft.EntityFrameworkCore;
using PMS.Application.DTOs.Common;
using PMS.Application.Interfaces.Repositories;
using PMS.Domain.Entities;
using PMS.Infrastructure.Dapper;
using PMS.Infrastructure.Data;
using TaskStatus = PMS.Domain.Enums.TaskStatus;

namespace PMS.Infrastructure.Repositories;

public class TaskRepository : GenericRepository<ProjectTask>, ITaskRepository
{
    private readonly DapperContext _dapper;

    public TaskRepository(ApplicationDbContext context, DapperContext dapper)
        : base(context)
    {
        _dapper = dapper;
    }

    public async Task<ProjectTask?> GetByIdWithDetailsAsync(int id)
        => await _context.Tasks
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Project)
            .Include(t => t.AssignedUser)
            .Include(t => t.TimeLogs
                .Where(l => !l.IsDeleted)
                .OrderByDescending(l => l.StartTime))
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<IEnumerable<ProjectTask>> GetByProjectIdAsync(int projectId)
        => await _context.Tasks
            .AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .Include(t => t.AssignedUser)
            .Select(t => new ProjectTask        // projection — only needed fields
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status,
                Priority = t.Priority,
                DueDate = t.DueDate,
                ProjectId = t.ProjectId,
                AssignedUserId = t.AssignedUserId,
                AssignedUser = t.AssignedUser,
                CreatedAt = t.CreatedAt,
                IsDeleted = t.IsDeleted
            })
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();



    public async Task<PagedResultDto<ProjectTask>> GetPagedAsync(
    QueryParameters parameters,
    int? projectId = null,
    TaskStatus? statusFilter = null)
    {
        var offset = (parameters.PageNumber - 1) * parameters.PageSize;

        var searchTerm = string.IsNullOrWhiteSpace(parameters.SearchTerm)
            ? null
            : $"%{parameters.SearchTerm.Trim()}%";

        var statusStr = statusFilter?.ToString();

        // ✅ SAFE SORTING (whitelist)
        var sortColumns = new Dictionary<string, string>
        {
            ["title"] = "t.Title",
            ["status"] = "t.Status",
            ["priority"] = "t.Priority",
            ["duedate"] = "t.DueDate",
            ["createdat"] = "t.CreatedAt"
        };

        var sortColumn = sortColumns.TryGetValue(
            parameters.SortBy?.ToLower() ?? "",
            out var col
        ) ? col : "t.CreatedAt";

        var sortDir = parameters.SortDesc ? "DESC" : "ASC";

        var sql = $@"
        SELECT
            t.Id, t.Title, t.Description,
            t.Status, t.Priority, t.DueDate,
            t.ProjectId, t.AssignedUserId,
            t.CreatedAt, t.UpdatedAt, t.IsDeleted,

            p.Id, p.Name,

            u.Id, u.FirstName, u.LastName, u.Email,

            (
                SELECT ISNULL(SUM(
                    DATEDIFF(SECOND, tl.StartTime, tl.EndTime)
                ), 0) / 3600.0
                FROM TaskTimeLogs tl
                WHERE tl.TaskId = t.Id
                  AND tl.EndTime IS NOT NULL
                  AND tl.IsDeleted = 0
            ) AS ComputedHours

        FROM Tasks t
        INNER JOIN Projects p ON p.Id = t.ProjectId AND p.IsDeleted = 0
        LEFT JOIN Users u ON u.Id = t.AssignedUserId AND u.IsDeleted = 0

        WHERE t.IsDeleted = 0
          AND (@ProjectId IS NULL OR t.ProjectId = @ProjectId)
          AND (@Status IS NULL OR t.Status = @Status)
          AND (@Search IS NULL
               OR t.Title LIKE @Search
               OR t.Description LIKE @Search)

        ORDER BY {sortColumn} {sortDir}
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

        SELECT COUNT(1)
        FROM Tasks t
        WHERE t.IsDeleted = 0
          AND (@ProjectId IS NULL OR t.ProjectId = @ProjectId)
          AND (@Status IS NULL OR t.Status = @Status)
          AND (@Search IS NULL
               OR t.Title LIKE @Search
               OR t.Description LIKE @Search);
    ";

        using var connection = _dapper.CreateConnection();

        using var multi = await connection.QueryMultipleAsync(
            sql,
            new
            {
                ProjectId = projectId,
                Status = statusStr,
                Search = searchTerm,
                Offset = offset,
                PageSize = parameters.PageSize
            });

        // ✅ FIX: Use Read (NOT ReadAsync for multi-mapping)
        var items = multi.Read<ProjectTask, Project, User, ProjectTask>(
            (task, project, user) =>
            {
                task.Project = project;
                task.AssignedUser = user;
                return task;
            },
            splitOn: "Id,Id"
        ).ToList();

        var total = await multi.ReadFirstAsync<int>();

        return new PagedResultDto<ProjectTask>
        {
            Items = items,
            TotalCount = total,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize
        };
    }


    //public async Task<PagedResultDto<ProjectTask>> GetPagedAsync(
    //    QueryParameters parameters,
    //    int? projectId = null,
    //    TaskStatus? statusFilter = null)
    //{
    //    var offset = (parameters.PageNumber - 1) * parameters.PageSize;
    //    var searchTerm = string.IsNullOrWhiteSpace(parameters.SearchTerm)
    //        ? null
    //        : $"%{parameters.SearchTerm.Trim()}%";

    //    var statusStr = statusFilter?.ToString();

    //    var sortColumn = parameters.SortBy?.ToLower() switch
    //    {
    //        "title" => "t.Title",
    //        "status" => "t.Status",
    //        "priority" => "t.Priority",
    //        "duedate" => "t.DueDate",
    //        "createdat" => "t.CreatedAt",
    //        _ => "t.CreatedAt"
    //    };

    //    var sortDir = parameters.SortDesc ? "DESC" : "ASC";

    //    // Dapper multi-mapping — Tasks + Project + User in one query
    //    var sql = $"""
    //        SELECT
    //            t.Id,          t.Title,       t.Description,
    //            t.Status,      t.Priority,    t.DueDate,
    //            t.ProjectId,   t.AssignedUserId,
    //            t.CreatedAt,   t.UpdatedAt,   t.IsDeleted,
    //            p.Id,          p.Name,
    //            u.Id,          u.FirstName,   u.LastName,  u.Email,
    //            (
    //                SELECT ISNULL(SUM(
    //                    DATEDIFF(SECOND, tl.StartTime, tl.EndTime)
    //                ), 0) / 3600.0
    //                FROM TaskTimeLogs tl
    //                WHERE tl.TaskId    = t.Id
    //                  AND tl.EndTime   IS NOT NULL
    //                  AND tl.IsDeleted = 0
    //            ) AS ComputedHours
    //        FROM   Tasks    t
    //        INNER JOIN Projects p ON p.Id = t.ProjectId  AND p.IsDeleted = 0
    //        LEFT  JOIN Users    u ON u.Id = t.AssignedUserId AND u.IsDeleted = 0
    //        WHERE  t.IsDeleted = 0
    //          AND  (@ProjectId IS NULL OR t.ProjectId = @ProjectId)
    //          AND  (@Status    IS NULL OR t.Status    = @Status)
    //          AND  (@Search    IS NULL
    //                OR t.Title       LIKE @Search
    //                OR t.Description LIKE @Search)
    //        ORDER BY {sortColumn} {sortDir}
    //        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    //        SELECT COUNT(1)
    //        FROM   Tasks t
    //        WHERE  t.IsDeleted = 0
    //          AND  (@ProjectId IS NULL OR t.ProjectId = @ProjectId)
    //          AND  (@Status    IS NULL OR t.Status    = @Status)
    //          AND  (@Search    IS NULL
    //                OR t.Title       LIKE @Search
    //                OR t.Description LIKE @Search);
    //        """;

    //    using var connection = _dapper.CreateConnection();
    //    using var multi = await connection.QueryMultipleAsync(
    //        sql,
    //        new
    //        {
    //            ProjectId = projectId,
    //            Status = statusStr,
    //            Search = searchTerm,
    //            Offset = offset,
    //            PageSize = parameters.PageSize
    //        },
    //        commandTimeout: 30);

    //    // Dapper multi-mapping: hydrate navigation properties manually
    //    var items = (await multi.ReadAsync<ProjectTask, Project, User, ProjectTask>(
    //        (task, project, user) =>
    //        {
    //            task.Project = project;
    //            task.AssignedUser = user;
    //            return task;
    //        },
    //        splitOn: "Id,Id"
    //    )).ToList();

    //    var total = await multi.ReadFirstAsync<int>();

    //    return new PagedResultDto<ProjectTask>
    //    {
    //        Items = items,
    //        TotalCount = total,
    //        PageNumber = parameters.PageNumber,
    //        PageSize = parameters.PageSize
    //    };
    //}
}


//using Dapper;
//using Microsoft.EntityFrameworkCore;
//using PMS.Application.DTOs.Common;
//using PMS.Application.Interfaces.Repositories;
//using PMS.Domain.Entities;
//using PMS.Infrastructure.Dapper;
//using PMS.Infrastructure.Data;
//using TaskStatus = PMS.Domain.Enums.TaskStatus;

//namespace PMS.Infrastructure.Repositories;

//public class TaskRepository : GenericRepository<ProjectTask>, ITaskRepository
//{
//    private readonly DapperContext _dapper;

//    public TaskRepository(ApplicationDbContext context, DapperContext dapper)
//        : base(context)
//    {
//        _dapper = dapper;
//    }

//    // EF Core — full graph for task detail view
//    public async Task<ProjectTask?> GetByIdWithDetailsAsync(int id)
//        => await _context.Tasks
//            .Include(t => t.Project)
//            .Include(t => t.AssignedUser)
//            .Include(t => t.TimeLogs.Where(l => !l.IsDeleted))
//            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

//    // EF Core — tasks per project
//    public async Task<IEnumerable<ProjectTask>> GetByProjectIdAsync(int projectId)
//        => await _context.Tasks
//            .Include(t => t.AssignedUser)
//            .Where(t => t.ProjectId == projectId && !t.IsDeleted)
//            .OrderByDescending(t => t.CreatedAt)
//            .ToListAsync();

//    // Dapper — fast paged read with optional filters
//    public async Task<PagedResultDto<ProjectTask>> GetPagedAsync(
//        QueryParameters parameters,
//        int? projectId = null,
//        TaskStatus? statusFilter = null)
//    {
//        var offset = (parameters.PageNumber - 1) * parameters.PageSize;
//        var searchTerm = string.IsNullOrWhiteSpace(parameters.SearchTerm)
//            ? null
//            : $"%{parameters.SearchTerm.Trim()}%";

//        var statusStr = statusFilter?.ToString();

//        var sortColumn = parameters.SortBy?.ToLower() switch
//        {
//            "title" => "t.Title",
//            "status" => "t.Status",
//            "priority" => "t.Priority",
//            "duedate" => "t.DueDate",
//            "createdat" => "t.CreatedAt",
//            _ => "t.CreatedAt"
//        };

//        var sortDir = parameters.SortDesc ? "DESC" : "ASC";

//        var sql = $"""
//            SELECT
//                t.*,
//                p.Name  AS ProjectName,
//                u.FirstName + ' ' + u.LastName AS AssignedUserName
//            FROM   Tasks t
//            INNER JOIN Projects p ON p.Id = t.ProjectId AND p.IsDeleted = 0
//            LEFT  JOIN Users   u ON u.Id = t.AssignedUserId AND u.IsDeleted = 0
//            WHERE  t.IsDeleted = 0
//              AND  (@ProjectId  IS NULL OR t.ProjectId = @ProjectId)
//              AND  (@Status     IS NULL OR t.Status    = @Status)
//              AND  (@Search     IS NULL
//                    OR t.Title       LIKE @Search
//                    OR t.Description LIKE @Search)
//            ORDER BY {sortColumn} {sortDir}
//            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

//            SELECT COUNT(*)
//            FROM   Tasks t
//            WHERE  t.IsDeleted = 0
//              AND  (@ProjectId IS NULL OR t.ProjectId = @ProjectId)
//              AND  (@Status    IS NULL OR t.Status    = @Status)
//              AND  (@Search    IS NULL
//                    OR t.Title       LIKE @Search
//                    OR t.Description LIKE @Search);
//            """;

//        using var connection = _dapper.CreateConnection();
//        using var multi = await connection.QueryMultipleAsync(sql, new
//        {
//            ProjectId = projectId,
//            Status = statusStr,
//            Search = searchTerm,
//            Offset = offset,
//            PageSize = parameters.PageSize
//        });

//        var items = (await multi.ReadAsync<ProjectTask>()).ToList();
//        var total = await multi.ReadFirstAsync<int>();

//        return new PagedResultDto<ProjectTask>
//        {
//            Items = items,
//            TotalCount = total,
//            PageNumber = parameters.PageNumber,
//            PageSize = parameters.PageSize
//        };
//    }
//}