namespace Cobryx.Domain.Shared;

public class Result
{
    public bool IsSuccess { get; }
    public DomainErrorCode? Error { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, DomainErrorCode? error)
    {
        if (isSuccess && error != null)
            throw new InvalidOperationException();
        if (!isSuccess && error == null)
            throw new InvalidOperationException();

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(DomainErrorCode error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result<T> Failure<T>(DomainErrorCode error) => new(default, false, error);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected internal Result(T? value, bool isSuccess, DomainErrorCode? error) : base(isSuccess, error)
    {
        Value = value;
    }
}
