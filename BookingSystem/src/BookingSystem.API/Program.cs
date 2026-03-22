using BookingSystem.API;
using BookingSystem.Core.Features.Bookings;
using BookingSystem.Core.Validators;
using BookingSystem.Infrastructure;
using BookingSystem.Infrastructure.Data;
using BookingSystem.Infrastructure.Services;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;

// ─── BOOTSTRAP ────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");
builder.Host.UseSerilog();

// ─── SERVICES ─────────────────────────────────────────────────────────────────
var dbPath = Path.Combine(AppContext.BaseDirectory, "bookingsystem.db");
builder.Services.AddInfrastructure(dbPath);

// MediatR - scans Core assembly for all handlers
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateBookingHandler).Assembly));

// FluentValidation pipeline behaviour
builder.Services.AddValidatorsFromAssemblyContaining<CreateBookingValidator>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Booking System API", Version = "v1",
        Description = "Real-time Booking & Order management system built with .NET 8, MediatR, EF Core + SQLite" });
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ── Register background workers inside the API process ────────────────────────
// This means one "dotnet run" starts EVERYTHING:
// API endpoints + EmailNotificationWorker + AnalyticsWorker
// They all share the same EventBridge (static, in-process) — no broker needed.
// In production these would be separate deployable services consuming RabbitMQ.
builder.Services.AddHostedService<BookingSystem.Worker.Workers.EmailNotificationWorker>();
builder.Services.AddHostedService<BookingSystem.Worker.Workers.AnalyticsWorker>();

var app = builder.Build();

// ─── MIDDLEWARE ───────────────────────────────────────────────────────────────
app.UseExceptionHandler();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Booking System v1");
    c.RoutePrefix = string.Empty; // Swagger at root
    c.DocumentTitle = "Booking System API";
});

// ─── AUTO CREATE DATABASE + SEED ─────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // EnsureCreated creates all tables directly from the model.
    // If the DB file already exists with tables, this is a no-op.
    db.Database.EnsureCreated();
    Log.Information("Database ready at {Path}", dbPath);
}

// ─── ENDPOINTS ────────────────────────────────────────────────────────────────
app.MapBookingEndpoints();
app.MapOrderEndpoints();
app.MapVenueEndpoints();
app.MapUtilityEndpoints();

Log.Information("=================================================");
Log.Information("  Booking System API is running!");
Log.Information("  Swagger UI  -> http://localhost:5000");
Log.Information("  Full Demo   -> POST http://localhost:5000/api/demo/full-flow");
Log.Information("=================================================");;
app.Run();

// ─── PIPELINE BEHAVIOURS ──────────────────────────────────────────────────────
public class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}

// Global exception handler — maps exception types to HTTP status codes
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            ValidationException        => (400, "Validation failed"),
            KeyNotFoundException       => (404, "Resource not found"),
            InvalidOperationException  => (409, "Operation not allowed"),
            _                          => (500, "An unexpected error occurred")
        };

        if (status >= 500)
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
        else
            logger.LogWarning("Request error [{Status}]: {Message}", status, ex.Message);

        var errors = ex is ValidationException ve
            ? ve.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage })
            : null;

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new
        {
            status,
            title,
            detail = ex.Message,
            errors
        }, ct);

        return true;
    }
}

public class LoggingBehaviour<TRequest, TResponse>(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation(">>> Handling {Request}", name);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await next();
        sw.Stop();
        logger.LogInformation("<<< Handled  {Request} in {Elapsed}ms", name, sw.ElapsedMilliseconds);
        return response;
    }
}
