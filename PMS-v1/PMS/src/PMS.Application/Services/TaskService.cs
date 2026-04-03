using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using PMS.Application.Constants;
using PMS.Application.DTOs.Common;
using PMS.Application.DTOs.Task;
using PMS.Application.Exceptions;
using PMS.Application.Interfaces;
using PMS.Application.Interfaces.Services;
using PMS.Domain.Entities;
using TaskStatus = PMS.Domain.Enums.TaskStatus;

namespace PMS.Application.Services;

public class TaskService : ITaskService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateTaskDto> _createValidator;
    private readonly IValidator<UpdateTaskDto> _updateValidator;
    private readonly ICacheService _cache;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        IUnitOfWork _uow,
        IMapper mapper,
        IValidator<CreateTaskDto> createValidator,
        IValidator<UpdateTaskDto> updateValidator,
        ICacheService cache,
        ILogger<TaskService> logger)
    {
        this._uow = _uow;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResultDto<ProjectTaskDto>> GetPagedAsync(
        QueryParameters parameters,
        int? projectId = null,
        TaskStatus? statusFilter = null)
    {
        var paged = await _uow.Tasks.GetPagedAsync(
            parameters, projectId, statusFilter);

        return new PagedResultDto<ProjectTaskDto>
        {
            Items = _mapper.Map<IEnumerable<ProjectTaskDto>>(paged.Items),
            TotalCount = paged.TotalCount,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize
        };
    }

    public async Task<ProjectTaskDto?> GetByIdAsync(int id)
    {
        var task = await _cache.GetOrCreateAsync(
            CacheKeys.Tasks.ById(id),
            async () => await _uow.Tasks.GetByIdWithDetailsAsync(id),
            absoluteExpiry: TimeSpan.FromMinutes(1));

        return task is null ? null : _mapper.Map<ProjectTaskDto>(task);
    }

    public async Task<ProjectTaskDto> CreateAsync(CreateTaskDto dto)
    {
        var result = await _createValidator.ValidateAsync(dto);
        if (!result.IsValid)
            throw new FluentValidation.ValidationException(result.Errors);

        var projectExists = await _uow.Projects
            .ExistsAsync(p => p.Id == dto.ProjectId && !p.IsDeleted);

        if (!projectExists)
            throw new NotFoundException("Project", dto.ProjectId);

        var entity = _mapper.Map<ProjectTask>(dto);
        await _uow.Tasks.AddAsync(entity);
        await _uow.SaveChangesAsync();

        // Invalidate project tasks cache
        _cache.RemoveByPrefix(CacheKeys.Tasks.Prefix);
        _cache.Remove(CacheKeys.Projects.ById(dto.ProjectId));

        _logger.LogInformation("Task created. Id:{TaskId} Title:{Title}",
            entity.Id, entity.Title);

        var created = await _uow.Tasks.GetByIdWithDetailsAsync(entity.Id);
        return _mapper.Map<ProjectTaskDto>(created!);
    }

    public async Task<ProjectTaskDto> UpdateAsync(UpdateTaskDto dto)
    {
        var result = await _updateValidator.ValidateAsync(dto);
        if (!result.IsValid)
            throw new FluentValidation.ValidationException(result.Errors);

        var entity = await _uow.Tasks.GetByIdAsync(dto.Id)
            ?? throw new NotFoundException("Task", dto.Id);

        var oldProjectId = entity.ProjectId;

        _mapper.Map(dto, entity);
        entity.UpdatedAt = DateTime.UtcNow;

        _uow.Tasks.Update(entity);
        await _uow.SaveChangesAsync();

        // Invalidate affected caches
        _cache.Remove(CacheKeys.Tasks.ById(dto.Id));
        _cache.Remove(CacheKeys.Projects.ById(oldProjectId));
        _cache.Remove(CacheKeys.Projects.ById(dto.ProjectId));

        _logger.LogInformation("Task updated. Id:{TaskId}", entity.Id);

        var updated = await _uow.Tasks.GetByIdWithDetailsAsync(entity.Id);
        return _mapper.Map<ProjectTaskDto>(updated!);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _uow.Tasks.GetByIdAsync(id)
            ?? throw new NotFoundException("Task", id);

        entity.SoftDelete();
        _uow.Tasks.Update(entity);
        await _uow.SaveChangesAsync();

        _cache.Remove(CacheKeys.Tasks.ById(id));
        _cache.Remove(CacheKeys.Projects.ById(entity.ProjectId));

        _logger.LogInformation("Task soft-deleted. Id:{TaskId}", id);
    }

    public async Task<IEnumerable<ProjectTaskDto>> GetByProjectIdAsync(int projectId)
        => await _cache.GetOrCreateAsync(
            CacheKeys.Tasks.ByProject(projectId),
            async () =>
            {
                var tasks = await _uow.Tasks.GetByProjectIdAsync(projectId);
                return _mapper.Map<IEnumerable<ProjectTaskDto>>(tasks);
            },
            absoluteExpiry: TimeSpan.FromMinutes(1));
}