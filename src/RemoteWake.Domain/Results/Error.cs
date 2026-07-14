namespace RemoteWake.Domain.Results;

public sealed record DomainError
{
    private DomainError(ErrorCode code, string reason, bool isRetryable)
    {
        Code = code;
        Reason = reason;
        IsRetryable = isRetryable;
    }

    public ErrorCode Code { get; }

    public string Reason { get; }

    public bool IsRetryable { get; }

    public static DomainError Create(ErrorCode code, string reason, bool isRetryable = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new DomainError(code, reason, isRetryable);
    }
}
