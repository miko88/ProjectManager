namespace ProjectManager.Application.Common;

public enum ResultStatus
{
    Success,
    Invalid,
    NotFound,
    Conflict,
    Unauthorized
}

/// <summary>
/// Explicit outcome of an application operation. Expected failures are modeled here;
/// exceptions are reserved for genuinely unexpected conditions.
/// </summary>
public class Result
{
    private static readonly IReadOnlyDictionary<string, string[]> NoErrors =
        new Dictionary<string, string[]>();

    public ResultStatus Status { get; }
    public string? Message { get; }
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }

    public bool IsSuccess => Status == ResultStatus.Success;

    protected Result(ResultStatus status, string? message, IReadOnlyDictionary<string, string[]>? validationErrors)
    {
        Status = status;
        Message = message;
        ValidationErrors = validationErrors ?? NoErrors;
    }

    public static Result Success() => new(ResultStatus.Success, null, null);
    public static Result NotFound(string message) => new(ResultStatus.NotFound, message, null);
    public static Result Conflict(string message) => new(ResultStatus.Conflict, message, null);
    public static Result Unauthorized(string message) => new(ResultStatus.Unauthorized, message, null);
    public static Result Invalid(IReadOnlyDictionary<string, string[]> errors)
        => new(ResultStatus.Invalid, null, errors);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(ResultStatus.Success, null, null) => Value = value;
    private Result(ResultStatus status, string? message, IReadOnlyDictionary<string, string[]>? errors)
        : base(status, message, errors) => Value = default;

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> NotFound(string message) => new(ResultStatus.NotFound, message, null);
    public static new Result<T> Conflict(string message) => new(ResultStatus.Conflict, message, null);
    public static new Result<T> Unauthorized(string message) => new(ResultStatus.Unauthorized, message, null);
    public static new Result<T> Invalid(IReadOnlyDictionary<string, string[]> errors)
        => new(ResultStatus.Invalid, null, errors);
}
