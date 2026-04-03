//namespace STHEnterprise.Api.Models; // or .Api.Common
//
//public class ApiResponse<T>
//{
//    public bool Success { get; init; }
//    public string Message { get; init; } = string.Empty;
//    public T? Data { get; init; }
//    public IEnumerable<string>? Errors { get; init; }
//
//    private ApiResponse() { }
//
//    public static ApiResponse<T> Ok(T? data, string message = "Success")
//        => new()
//        {
//            Success = true,
//            Message = message,
//            Data = data
//        };
//
//    public static ApiResponse<T> Fail(
//        string message,
//        IEnumerable<string>? errors = null)
//        => new()
//        {
//            Success = false,
//            Message = message,
//            Errors = errors
//        };
//}



//public static class ApiResponse
//{
//    public static ApiResponse<object> Success(string message)
//        => ApiResponse<object>.Ok(null, message);

//    public static ApiResponse<T> Success<T>(T data, string message = "Success")
//        => ApiResponse<T>.Ok(data, message);

//    public static ApiResponse<object> Fail(string message)
//        => ApiResponse<object>.Fail(message);
//}



public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public object? Data { get; set; }

}

//public class ApiResponse<T>
//{
//    public bool Success { get; init; }
//    public string Message { get; init; } = string.Empty;
//    public T? Data { get; init; }
//    public IEnumerable<string>? Errors { get; init; }

//    private ApiResponse() { }

//    public static ApiResponse<T> Ok(T? data, string message = "Success")
//        => new()
//        {
//            Success = true,
//            Message = message,
//            Data = data
//        };

//    public static ApiResponse<T> Fail(
//        string message,
//        IEnumerable<string>? errors = null)
//        => new()
//        {
//            Success = false,
//            Message = message,
//            Errors = errors
//        };
//}
