namespace PMS.Application.Exceptions;

/// <summary>
/// Thrown when an operation conflicts with existing state.
/// Maps to HTTP 409.
/// </summary>
public class ConflictException : ApplicationException
{
    //public ConflictException(string message)
    //    : base(
    //        title: "Conflict",
    //        message: message,
    //        statusCode: StatusCodes.Status409Conflict)
    //{
    //}

    public ConflictException(string message)
        : base(
            title: "Conflict",
            message: message,
            statusCode: 409) // ✅ use numeric value
    {
    }
}