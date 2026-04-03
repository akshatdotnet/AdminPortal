namespace PMS.Web.Middleware;

/// <summary>
/// Assigns a unique Correlation ID to every request.
/// Propagates it in the response header and Serilog context
/// so all log entries for a request share the same ID.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Honour caller-supplied ID (useful for distributed tracing)
        var correlationId = context.Request.Headers
            .TryGetValue(CorrelationIdHeader, out var existing)
                ? existing.ToString()
                : Guid.NewGuid().ToString("N")[..16]; // compact 16-char ID

        // Make available via HttpContext.Items
        context.Items["CorrelationId"] = correlationId;

        // Echo back in response
        context.Response.OnStarting(() =>
        {
            context.Response.Headers
                .TryAdd(CorrelationIdHeader, correlationId);
            return Task.CompletedTask;
        });

        // Push into Serilog context — every log entry will carry this
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            _logger.LogDebug("Request {Method} {Path} [CorrelationId: {CorrelationId}]",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            await _next(context);
        }
    }
}