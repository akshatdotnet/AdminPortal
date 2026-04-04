namespace Zovo.Core.ValueObjects;

public sealed class PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
    public int TotalCount       { get; init; }
    public int Page             { get; init; }
    public int PageSize         { get; init; }
    public int TotalPages       => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage     => Page < TotalPages;

    public static PagedResult<T> Create(IEnumerable<T> items, int total, int page, int size)
        => new() { Items = items, TotalCount = total, Page = page, PageSize = size };
}

public sealed class Result
{
    public bool    IsSuccess { get; }
    public string  Message   { get; }
    public string? ErrorCode { get; }

    private Result(bool ok, string msg, string? code = null)
        => (IsSuccess, Message, ErrorCode) = (ok, msg, code);

    public static Result Ok(string message = "Success")              => new(true,  message);
    public static Result Fail(string message, string? code = null)   => new(false, message, code);
}

public sealed class Result<T>
{
    public bool    IsSuccess { get; }
    public string  Message   { get; }
    public T?      Value     { get; }
    public string? ErrorCode { get; }

    private Result(bool ok, string msg, T? value, string? code = null)
        => (IsSuccess, Message, Value, ErrorCode) = (ok, msg, value, code);

    public static Result<T> Ok(T value, string message = "Success")  => new(true,  message, value);
    public static Result<T> Fail(string message, string? code = null)=> new(false, message, default, code);
}
