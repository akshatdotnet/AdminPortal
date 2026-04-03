namespace PMS.Application.Exceptions;

/// <summary>
/// Thrown when a requested resource does not exist.
/// Maps to HTTP 404.
/// </summary>
public class NotFoundException : ApplicationException
{
    public NotFoundException(string resourceName, object key)
        : base(
            title: "Resource Not Found",
            message: $"{resourceName} with identifier '{key}' was not found.",
            statusCode: 404) // ✅ FIXED
    {
    }

    public NotFoundException(string message)
        : base(
            title: "Resource Not Found",
            message: message,
            statusCode: 404) // ✅ FIXED
    {
    }

}