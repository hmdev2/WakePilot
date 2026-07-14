namespace RemoteWake.Domain.Identifiers;

public abstract record StrongId
{
    protected StrongId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("An identifier cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public sealed override string ToString() => Value.ToString("D");
}

public sealed record ComputerId : StrongId
{
    private ComputerId(Guid value)
        : base(value)
    {
    }

    public static ComputerId New() => new(Guid.NewGuid());

    public static ComputerId From(Guid value) => new(value);
}

public sealed record BridgeId : StrongId
{
    private BridgeId(Guid value)
        : base(value)
    {
    }

    public static BridgeId New() => new(Guid.NewGuid());

    public static BridgeId From(Guid value) => new(value);
}

public sealed record RequestId : StrongId
{
    private RequestId(Guid value)
        : base(value)
    {
    }

    public static RequestId New() => new(Guid.NewGuid());

    public static RequestId From(Guid value) => new(value);
}

public sealed record TargetId : StrongId
{
    private TargetId(Guid value)
        : base(value)
    {
    }

    public static TargetId New() => new(Guid.NewGuid());

    public static TargetId From(Guid value) => new(value);
}

public sealed record CorrelationId : StrongId
{
    private CorrelationId(Guid value)
        : base(value)
    {
    }

    public static CorrelationId New() => new(Guid.NewGuid());

    public static CorrelationId From(Guid value) => new(value);
}
