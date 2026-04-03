namespace ECommerce.Application.Common.Models;

/// <summary>
/// Functional Result type — encapsulates success/failure without exceptions.
/// Follows Railway-Oriented Programming pattern.
/// </summary>
public class Result<T>
{
    public T? Value { get; }
    public string Error { get; } = string.Empty;
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private Result(T value) { Value = value; IsSuccess = true; }
    private Result(string error) { Error = error; IsSuccess = false; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);
}

public static class Result
{
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
}
