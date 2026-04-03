using AutoMapper;
using PMS.Application.DTOs.Project;
using PMS.Application.DTOs.Task;
using PMS.Application.DTOs.TimeLog;
using PMS.Domain.Entities;

namespace PMS.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        ConfigureProjectMappings();
        ConfigureTaskMappings();
        ConfigureTimeLogMappings();
    }

    private void ConfigureProjectMappings()
    {
        CreateMap<Project, ProjectDto>()
            .ForMember(d => d.TotalTasks,
                o => o.MapFrom(s => s.Tasks.Count(t => !t.IsDeleted)))
            .ForMember(d => d.CompletedTasks,
                o => o.MapFrom(s => s.Tasks.Count(t =>
                    !t.IsDeleted &&
                    t.Status == Domain.Enums.TaskStatus.Completed)))
            .ForMember(d => d.CompletionPercentage,
                o => o.MapFrom(s => s.CompletionPercentage));

        CreateMap<CreateProjectDto, Project>();

        CreateMap<UpdateProjectDto, Project>()
            .ForMember(d => d.Id, o => o.Ignore()); // Id set by service
    }

    private void ConfigureTaskMappings()
    {
        CreateMap<ProjectTask, ProjectTaskDto>()
            .ForMember(d => d.ProjectName,
                o => o.MapFrom(s => s.Project != null ? s.Project.Name : string.Empty))
            .ForMember(d => d.AssignedUserName,
                o => o.MapFrom(s => s.AssignedUser != null
                    ? s.AssignedUser.FullName
                    : null))
            .ForMember(d => d.TotalHoursLogged,
                o => o.MapFrom(s => s.TotalHoursLogged))
            .ForMember(d => d.HasActiveTimer,
                o => o.MapFrom(s => s.HasActiveTimer));

        CreateMap<CreateTaskDto, ProjectTask>();

        CreateMap<UpdateTaskDto, ProjectTask>()
            .ForMember(d => d.Id, o => o.Ignore());
    }

    private void ConfigureTimeLogMappings()
    {
        CreateMap<TaskTimeLog, TaskTimeLogDto>()
            .ForMember(d => d.TaskTitle,
                o => o.MapFrom(s => s.Task != null ? s.Task.Title : string.Empty))
            .ForMember(d => d.FormattedDuration,
                o => o.MapFrom(s => s.FormattedDuration))
            .ForMember(d => d.IsRunning,
                o => o.MapFrom(s => s.IsRunning))
            .ForMember(d => d.TotalHours,
                o => o.MapFrom(s => s.TotalHours));
    }
}