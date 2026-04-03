using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PMS.Application.Interfaces.Services;
using PMS.Application.Services;
using System.Reflection;

namespace PMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Services — Scoped (per HTTP request)
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITimeLogService, TimeLogService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}



//using FluentValidation;
//using Microsoft.Extensions.DependencyInjection;
//using PMS.Application.Interfaces.Services;
//using PMS.Application.Services;
//using System.Reflection;

//namespace PMS.Application;

//public static class DependencyInjection
//{
//    public static IServiceCollection AddApplication(this IServiceCollection services)
//    {
//        // AutoMapper
//        services.AddAutoMapper(Assembly.GetExecutingAssembly());

//        // FluentValidation — auto-registers all validators in this assembly
//        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

//        // Services
//        services.AddScoped<IProjectService, ProjectService>();
//        services.AddScoped<ITaskService, TaskService>();
//        services.AddScoped<ITimeLogService, TimeLogService>();
//        services.AddScoped<IUserService, UserService>();


//        return services;
//    }
//}
