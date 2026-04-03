using System.Diagnostics;

namespace PMS.Web.Middleware;

/// <summary>
/// Logs a warning for any request that takes longer than the threshold.
/// Helps identify slow endpoints early.
/// </summary>
public class PerformanceLoggingMiddleware
{
    private const int WarningThresholdMs = 500;
    private const int CriticalThresholdMs = 2000;

    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceLoggingMiddleware> _logger;

    public PerformanceLoggingMiddleware(
        RequestDelegate next,
        ILogger<PerformanceLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        await _next(context);

        sw.Stop();
        var elapsed = sw.ElapsedMilliseconds;

        if (elapsed >= CriticalThresholdMs)
        {
            _logger.LogError(
                "CRITICAL SLOW REQUEST — {Method} {Path} took {ElapsedMs}ms " +
                "(threshold: {ThresholdMs}ms) | Status: {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                elapsed,
                CriticalThresholdMs,
                context.Response.StatusCode);
        }
        else if (elapsed >= WarningThresholdMs)
        {
            _logger.LogWarning(
                "SLOW REQUEST — {Method} {Path} took {ElapsedMs}ms " +
                "(threshold: {ThresholdMs}ms)",
                context.Request.Method,
                context.Request.Path,
                elapsed,
                WarningThresholdMs);
        }
    }
}