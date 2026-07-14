namespace RemoteWake.Domain.Results;

public sealed class Result
{
    private Result(bool isSuccess, DomainError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public DomainError? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(DomainError error) => new(false, error ?? throw new ArgumentNullException(nameof(error)));

    public static Result<T> Success<T>(T value) => Result<T>.CreateSuccess(value);

    public static Result<T> Failure<T>(DomainError error) => Result<T>.CreateFailure(error);
}

public sealed class Result<T>
{
    private readonly T? value;

    internal Result(bool isSuccess, T? value, DomainError? error)
    {
        IsSuccess = isSuccess;
        this.value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public DomainError? Error { get; }

    public T Value => IsSuccess
        ? value!
        : throw new InvalidOperationException("A failed result does not contain a value.");

    internal static Result<T> CreateSuccess(T value) =>
        new(true, value ?? throw new ArgumentNullException(nameof(value)), null);

    internal static Result<T> CreateFailure(DomainError error) =>
        new(false, default, error ?? throw new ArgumentNullException(nameof(error)));
}
