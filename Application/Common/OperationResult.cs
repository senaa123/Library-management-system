namespace LibraryM.Application.Common;

public enum FailureType
{
    Validation,
    Unauthorized,
    NotFound,
    Conflict
}

public class OperationResult
{
    protected OperationResult(bool isSuccess, string? message, FailureType? failureType)
    {
        IsSuccess = isSuccess;
        Message = message;
        FailureType = failureType;
    }

    public bool IsSuccess { get; }

    public string? Message { get; }

    public FailureType? FailureType { get; }

    public static OperationResult Success(string? message = null) => new(true, message, null);

    public static OperationResult Failure(string message, FailureType failureType) => new(false, message, failureType);
}

public sealed class OperationResult<T> : OperationResult
{
    private OperationResult(bool isSuccess, T? value, string? message, FailureType? failureType)
        : base(isSuccess, message, failureType)
    {
        Value = value;
    }

    public T? Value { get; }

    public static OperationResult<T> Success(T value, string? message = null) => new(true, value, message, null);

    public new static OperationResult<T> Failure(string message, FailureType failureType) => new(false, default, message, failureType);
}
