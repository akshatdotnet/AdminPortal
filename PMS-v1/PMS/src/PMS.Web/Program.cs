using PMS.Application;
using PMS.Infrastructure;
using PMS.Infrastructure.Data;
using PMS.Infrastructure.Data.Seed;
using PMS.Web.Extensions;
using PMS.Web.Filters;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting PMS application");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, config) =>
    {
        config
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Application", "PMS")
            .WriteTo.Console(
                outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] " +
                    "{CorrelationId:l} | {Message:lj}" +
                    "{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Debug)
            .WriteTo.File(
                path: "Logs/pms-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] " +
                    "[{CorrelationId}] [{MachineName}] " +
                    "{Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "Logs/pms-errors-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 90,
                restrictedToMinimumLevel: LogEventLevel.Error,
                outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] " +
                    "[{CorrelationId}] {Message:lj}" +
                    "{NewLine}{Exception}");
    });

    // ── Layer registrations ───────────────────────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddWebServices();

    // ── MVC ───────────────────────────────────────────────────────────────────
    builder.Services.AddControllersWithViews(opts =>
    {
        opts.Filters.Add<ValidateModelAttribute>();
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddResponseCompression();

    var app = builder.Build();

    // ── Middleware pipeline ───────────────────────────────────────────────────
    app.UsePmsMiddleware();          // Correlation → Performance → Exceptions

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseResponseCompression();
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            // Cache static files for 7 days
            ctx.Context.Response.Headers.Append(
                "Cache-Control", "public,max-age=604800");
        }
    });

    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} " +
            "responded {StatusCode} in {Elapsed:0.0000}ms";

        opts.GetLevel = (ctx, elapsed, ex) =>
            ex is not null || ctx.Response.StatusCode >= 500
                ? LogEventLevel.Error
                : elapsed > 1000
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;

        opts.EnrichDiagnosticContext = (diag, ctx) =>
        {
            diag.Set("RemoteIp", ctx.Connection.RemoteIpAddress?.ToString());
            diag.Set("UserAgent", ctx.Request.Headers.UserAgent.ToString());
            diag.Set("CorrelationId",
                ctx.Items["CorrelationId"]?.ToString() ?? "none");
        };
    });

    app.UseRouting();
    app.UseResponseCaching();
    app.UseAuthorization();

    app.MapHealthChecks("/health");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    // ── Database seed ─────────────────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider
                          .GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider
                          .GetRequiredService<ILogger<Program>>();
        await DatabaseSeeder.SeedAsync(db, logger);
    }

    Log.Information("PMS application started successfully on {Env}",
        builder.Environment.EnvironmentName);

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "PMS host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

