using ECommerce.Application.Common.Models;
using ECommerce.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace ECommerce.Application.Common.Behaviours;

/// <summary>
/// Pipeline behaviour that catches DomainExceptions and converts them into
/// Result.Failure responses rather than letting them propagate as unhandled exceptions.
///
/// This keeps domain rule violations in the Result monad where callers expect them,
/// instead of crashing the entire call stack.
/// </summary>
public class ExceptionHandlingBehaviour<TRequest, TResponse>(
    ILogger<ExceptionHandlingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        try
        {
            return await next();
        }
        catch (DomainException ex)
        {
            logger.LogWarning("Domain rule violation in {Request}: {Message}",
                typeof(TRequest).Name, ex.Message);

            // If TResponse is Result<T>, wrap the exception message into Result.Failure
            // instead of letting the exception propagate to the caller.
            var failure = TryCreateFailureResult(ex.Message);
            if (failure is TResponse typed)
                return typed;

            // TResponse is not a Result<T> — re-throw so the caller sees the exception
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in {Request}", typeof(TRequest).Name);
            throw;
        }
    }

    /// <summary>
    /// Attempts to create a Result&lt;T&gt;.Failure(message) for any Result&lt;T&gt; response type.
    /// Returns null if TResponse is not a generic Result&lt;T&gt;.
    /// </summary>
    private static object? TryCreateFailureResult(string message)
    {
        var responseType = typeof(TResponse);
        if (!responseType.IsGenericType) return null;
        if (responseType.GetGenericTypeDefinition() != typeof(Result<>)) return null;

        // Result<T> has a static Failure(string) method
        var innerType = responseType.GetGenericArguments()[0];
        var resultClass = typeof(Result);
        var failureMethod = resultClass
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m =>
                m.Name == "Failure" &&
                m.IsGenericMethodDefinition &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == typeof(string));

        if (failureMethod is null) return null;
        return failureMethod.MakeGenericMethod(innerType).Invoke(null, [message]);
    }
}
