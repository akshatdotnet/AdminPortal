namespace PMS.Application.Exceptions;

/// <summary>
/// Base class for all application-level exceptions.
/// Separates business rule violations from infrastructure errors.
/// </summary>
public abstract class ApplicationException : Exception
{
    public string Title { get; }
    public int StatusCode { get; }

    protected ApplicationException(
        string title,
        string message,
        int statusCode,
        Exception? inner = null)
        : base(message, inner)
    {
        Title = title;
        StatusCode = statusCode;
    }
}