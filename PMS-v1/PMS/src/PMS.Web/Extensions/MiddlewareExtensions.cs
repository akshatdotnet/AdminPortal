using PMS.Web.Middleware;

namespace PMS.Web.Extensions;

public static class MiddlewareExtensions
{
    /// <summary>
    /// Registers all PMS custom middleware in the correct order.
    /// Call this once in Program.cs instead of adding each middleware manually.
    /// </summary>
    public static IApplicationBuilder UsePmsMiddleware(
        this IApplicationBuilder app)
    {
        // 1. Correlation ID — must be first so all subsequent logs carry it
        app.UseMiddleware<CorrelationIdMiddleware>();

        // 2. Performance logging — wraps all downstream processing
        app.UseMiddleware<PerformanceLoggingMiddleware>();

        // 3. Exception handling — catches anything thrown downstream
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        return app;
    }
}