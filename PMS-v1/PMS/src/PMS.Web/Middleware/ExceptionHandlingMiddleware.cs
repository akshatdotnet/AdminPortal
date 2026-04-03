using System.Net.Mime;
using System.Text.Json;
using FluentValidation;
using PMS.Application.Exceptions;
using ApplicationException = PMS.Application.Exceptions.ApplicationException;

namespace PMS.Web.Middleware;

/// <summary>
/// Central exception handler — catches all unhandled exceptions,
/// logs them with full context, and returns a structured error response.
///
/// For AJAX requests  → JSON Problem Details (RFC 7807)
/// For page requests  → redirect to Error view
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    // ── Exception → Response mapping ─────────────────────────────────────────
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown";
        var isAjax = IsAjaxRequest(context);

        // ── Log with full context ─────────────────────────────────────────────
        LogException(exception, context, correlationId);

        // ── Build error details ───────────────────────────────────────────────
        var (statusCode, title, detail, errors) = MapException(exception);

        // ── Respond ───────────────────────────────────────────────────────────
        if (isAjax)
        {
            await WriteJsonResponseAsync(
                context, statusCode, title, detail,
                correlationId, errors, exception);
        }
        else
        {
            await WriteHtmlResponseAsync(
                context, statusCode, title, detail, correlationId);
        }
    }

    // ── Map exception type to HTTP status ─────────────────────────────────────
    private (int status, string title, string detail,
             IDictionary<string, string[]>? errors)
        MapException(Exception ex)
    {
        return ex switch
        {
            // Application-defined exceptions
            PMS.Application.Exceptions.ValidationException ve =>
                (ve.StatusCode, ve.Title, ve.Message, ve.Errors),

            NotFoundException ne =>
                (ne.StatusCode, ne.Title, ne.Message, null),

            ConflictException ce =>
                (ce.StatusCode, ce.Title, ce.Message, null),

            ForbiddenException fe =>
                (fe.StatusCode, fe.Title, fe.Message, null),

            ApplicationException ae =>
                (ae.StatusCode, ae.Title, ae.Message, null),

            // FluentValidation (from direct validator calls)
            FluentValidation.ValidationException fve =>
            (
                StatusCodes.Status422UnprocessableEntity,
                "Validation Failed",
                "One or more validation errors occurred.",
                fve.Errors
                   .GroupBy(e => e.PropertyName)
                   .ToDictionary(
                       g => g.Key,
                       g => g.Select(e => e.ErrorMessage).ToArray())
            ),

            // Not found (e.g., from KeyNotFoundException)
            KeyNotFoundException =>
                (StatusCodes.Status404NotFound,
                 "Not Found", ex.Message, null),

            // Operation cancelled (user navigated away)
            OperationCanceledException =>
                (StatusCodes.Status499ClientClosedRequest,
                 "Request Cancelled", "The request was cancelled.", null),

            // Everything else → 500
            _ => (StatusCodes.Status500InternalServerError,
                  "Internal Server Error",
                  "An unexpected error occurred. Please try again.", null)
        };
    }

    // ── Write JSON (AJAX / API) ───────────────────────────────────────────────
    private async Task WriteJsonResponseAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string correlationId,
        IDictionary<string, string[]>? errors,
        Exception exception)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = MediaTypeNames.Application.Json;

        object payload = errors is not null
            ? new
            {
                success = false,
                title,
                detail,
                correlationId,
                validationErrors = errors
            }
            : new
            {
                success = false,
                title,
                message = detail,
                correlationId,
                // Stack trace only in development
                stackTrace = _env.IsDevelopment() ? exception.StackTrace : null
            };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, JsonOptions));
    }

    // ── Write HTML (page request) ─────────────────────────────────────────────
    private static async Task WriteHtmlResponseAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string correlationId)
    {
        context.Response.StatusCode = statusCode;

        if (statusCode == StatusCodes.Status404NotFound)
        {
            context.Response.Redirect("/Home/NotFound");
            return;
        }

        // Store for Error.cshtml to display
        context.Items["ErrorTitle"] = title;
        context.Items["ErrorDetail"] = detail;
        context.Items["ErrorCorrelationId"] = correlationId;

        context.Response.Redirect("/Home/Error");
    }

    // ── Structured logging per exception type ─────────────────────────────────
    private void LogException(
        Exception ex,
        HttpContext context,
        string correlationId)
    {
        var meta = new
        {
            CorrelationId = correlationId,
            Method = context.Request.Method,
            Path = context.Request.Path.Value,
            QueryString = context.Request.QueryString.Value,
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            RemoteIp = context.Connection.RemoteIpAddress?.ToString()
        };

        switch (ex)
        {
            // Expected / business errors — Info level
            case PMS.Application.Exceptions.ValidationException:
            case FluentValidation.ValidationException:
            case NotFoundException:
                _logger.LogInformation(ex,
                    "Business rule violation. " +
                    "CorrelationId: {CorrelationId} | {Method} {Path}",
                    meta.CorrelationId, meta.Method, meta.Path);
                break;

            // Conflict / Forbidden — Warning level
            case ConflictException:
            case ForbiddenException:
                _logger.LogWarning(ex,
                    "Request conflict or access denied. " +
                    "CorrelationId: {CorrelationId} | {Method} {Path}",
                    meta.CorrelationId, meta.Method, meta.Path);
                break;

            // Cancelled — Debug (not an error)
            case OperationCanceledException:
                _logger.LogDebug(
                    "Request cancelled by client. " +
                    "CorrelationId: {CorrelationId} | {Method} {Path}",
                    meta.CorrelationId, meta.Method, meta.Path);
                break;

            // Everything else — Error level with full context
            default:
                _logger.LogError(ex,
                    "Unhandled exception. " +
                    "CorrelationId: {CorrelationId} | " +
                    "{Method} {Path}{QueryString} | " +
                    "IP: {RemoteIp}",
                    meta.CorrelationId,
                    meta.Method,
                    meta.Path,
                    meta.QueryString,
                    meta.RemoteIp);
                break;
        }
    }

    // ── AJAX detection ────────────────────────────────────────────────────────
    private static bool IsAjaxRequest(HttpContext context)
        => context.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
        || context.Request.Headers["Accept"]
                  .ToString().Contains("application/json");
}