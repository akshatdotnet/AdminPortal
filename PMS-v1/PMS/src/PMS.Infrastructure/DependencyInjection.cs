using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PMS.Application.Interfaces;
using PMS.Infrastructure.Dapper;
using PMS.Infrastructure.Data;
using PMS.Infrastructure.UnitOfWork;

namespace PMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── EF Core ───────────────────────────────────────────────────────────
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(
                        typeof(ApplicationDbContext).Assembly.FullName);

                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);

                    sqlOptions.CommandTimeout(60);
                });
        });

        // ── Dapper ────────────────────────────────────────────────────────────
        services.AddSingleton<DapperContext>();

        // ── Unit of Work ──────────────────────────────────────────────────────
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

        // ── Health Check ──────────────────────────────────────────────────────
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "sql" });

        // ── Auto-migration on startup ──────────────────────────────────────────
        services.AddHostedService<MigrationService>();

        return services;
    }
}


//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Diagnostics.HealthChecks;
//using PMS.Application.Interfaces;
//using PMS.Infrastructure.Dapper;
//using PMS.Infrastructure.Data;  
//using PMS.Infrastructure.UnitOfWork;

//namespace PMS.Infrastructure;

//public static class DependencyInjection
//{
//    public static IServiceCollection AddInfrastructure(
//        this IServiceCollection services,
//        IConfiguration configuration)
//    {
//        // ── EF Core ───────────────────────────────────────────────────────────
//        services.AddDbContext<ApplicationDbContext>(options =>
//        {
//            options.UseSqlServer(
//                configuration.GetConnectionString("DefaultConnection"),
//                sqlOptions =>
//                {
//                    sqlOptions.MigrationsAssembly(
//                        typeof(ApplicationDbContext).Assembly.FullName);

//                    sqlOptions.EnableRetryOnFailure(
//                        maxRetryCount: 5,
//                        maxRetryDelay: TimeSpan.FromSeconds(30),
//                        errorNumbersToAdd: null);

//                    sqlOptions.CommandTimeout(60);
//                });
//        });

//        // ── Dapper ────────────────────────────────────────────────────────────
//        services.AddSingleton<DapperContext>();

//        // ── Unit of Work ──────────────────────────────────────────────────────
//        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

//        // ── Health Check ──────────────────────────────────────────────────────
//        services.AddHealthChecks()
//            .AddCheck<DatabaseHealthCheck>(
//                name: "database",
//                failureStatus: HealthStatus.Unhealthy,
//                tags: new[] { "db", "sql" });

//        // ── Auto-migration on startup ──────────────────────────────────────────
//        services.AddHostedService<MigrationService>();

//        return services;
//    }
//}