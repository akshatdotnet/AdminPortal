using Common.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Common.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleAsync(context, ex);
        }
    }

    private static async Task HandleAsync(HttpContext ctx, Exception ex)
    {
        var (status, title, detail, errors) = ex switch
        {
            ValidationException vex => (400, "Validation Error",
                "One or more validation errors occurred.",
                (object?)vex.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
            NotFoundException nex   => (404, "Not Found",       nex.Message, null),
            BusinessRuleException b => (422, "Business Rule",   b.Message,   null),
            ConflictException c     => (409, "Conflict",        c.Message,   null),
            UnauthorizedAccessException u => (401, "Unauthorized", u.Message, null),
            _                       => (500, "Server Error",
                "An unexpected error occurred.", null)
        };

        ctx.Response.ContentType = "application/problem+json";
        ctx.Response.StatusCode  = status;

        var body = new { type = $"https://httpstatuses.com/{status}", title, status, detail, errors };
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(body, Options));
    }
}
