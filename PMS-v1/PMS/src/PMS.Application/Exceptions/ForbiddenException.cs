namespace PMS.Application.Exceptions;

/// <summary>
/// Thrown when the user lacks permission for an operation.
/// Maps to HTTP 403.
/// </summary>
public class ForbiddenException : ApplicationException
{


    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(
            title: "Forbidden",
            message: message,
            statusCode: 403) // ✅ FIXED
    {
    }


    //public ForbiddenException(string message = "You do not have permission to perform this action.")
    //    : base(
    //        title: "Forbidden",
    //        message: message,
    //        statusCode: StatusCodes.Status403Forbidden)
    //{
    //}
}