using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using PMS.Application.DTOs.Common;
using PMS.Application.DTOs.Project;
using PMS.Application.Interfaces;
using PMS.Application.Interfaces.Services;
using PMS.Domain.Entities;

namespace PMS.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateProjectDto> _createValidator;
    private readonly IValidator<UpdateProjectDto> _updateValidator;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        IUnitOfWork uow,
        IMapper mapper,
        IValidator<CreateProjectDto> createValidator,
        IValidator<UpdateProjectDto> updateValidator
        ,ILogger<ProjectService> logger)
    {
        _uow = uow;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    public async Task<PagedResultDto<ProjectDto>> GetPagedAsync(QueryParameters parameters)
    {
       _logger.LogInformation("Fetching paged projects. Page: {Page}, Size: {Size}, Search: {Search}",
           parameters.PageNumber, parameters.PageSize, parameters.SearchTerm);

        var paged = await _uow.Projects.GetPagedAsync(parameters);

        return new PagedResultDto<ProjectDto>
        {
            Items = _mapper.Map<IEnumerable<ProjectDto>>(paged.Items),
            TotalCount = paged.TotalCount,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize
        };
    }

    public async Task<ProjectDto?> GetByIdAsync(int id)
    {
        var project = await _uow.Projects.GetByIdWithTasksAsync(id);
        return project is null ? null : _mapper.Map<ProjectDto>(project);
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto dto)
    {
        // Validate
        var result = await _createValidator.ValidateAsync(dto);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        var entity = _mapper.Map<Project>(dto);
        await _uow.Projects.AddAsync(entity);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Project created. Id: {ProjectId}, Name: {Name}",
            entity.Id, entity.Name);

        return _mapper.Map<ProjectDto>(entity);
    }

    public async Task<ProjectDto> UpdateAsync(UpdateProjectDto dto)
    {
        var result = await _updateValidator.ValidateAsync(dto);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        var entity = await _uow.Projects.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException($"Project {dto.Id} not found.");

        _mapper.Map(dto, entity);
        entity.UpdatedAt = DateTime.UtcNow;

        _uow.Projects.Update(entity);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Project updated. Id: {ProjectId}", entity.Id);

        return _mapper.Map<ProjectDto>(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _uow.Projects.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Project {id} not found.");

        entity.SoftDelete();
        _uow.Projects.Update(entity);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Project soft-deleted. Id: {ProjectId}", id);
    }

    public async Task<IEnumerable<ProjectDto>> GetAllActiveAsync()
    {
        var projects = await _uow.Projects.FindAsync(p =>
            !p.IsDeleted &&
            p.Status == Domain.Enums.ProjectStatus.Active);

        return _mapper.Map<IEnumerable<ProjectDto>>(projects);
    }
}