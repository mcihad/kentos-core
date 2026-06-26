namespace Kentos.SharedKernel.Results;

/// <summary>
/// Lightweight result type. The standard error path is exception -&gt; ProblemDetails;
/// this type is for internal domain logic that prefers flow control over throwing.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result<T> Failure<T>(string error) => new(default, false, error);
}

/// <inheritdoc />
public class Result<T> : Result
{
    internal Result(T? value, bool isSuccess, string? error) : base(isSuccess, error) => Value = value;

    public T? Value { get; }
}
