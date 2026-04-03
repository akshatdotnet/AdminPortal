namespace PMS.Application.Exceptions;

/// <summary>
/// Thrown when input fails business rule validation.
/// Maps to HTTP 422 Unprocessable Entity.
/// </summary>
public class ValidationException : ApplicationException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base(
            title: "Validation Failed",
            message: "One or more validation errors occurred.",
            statusCode: 422) // ✅ FIXED
    {
        Errors = errors;
    }

    public ValidationException(string field, string error)
        : this(new Dictionary<string, string[]>
        {
            [field] = new[] { error }
        })
    {
    }
}