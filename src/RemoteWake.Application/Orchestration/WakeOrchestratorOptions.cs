namespace RemoteWake.Application.Orchestration;

public sealed record WakeOrchestratorOptions
{
    public TimeSpan BridgeTimeout { get; init; } = TimeSpan.FromSeconds(5);

    public TimeSpan ReceiptTimeout { get; init; } = TimeSpan.FromSeconds(10);

    public TimeSpan WindowsTimeout { get; init; } = TimeSpan.FromSeconds(240);

    public TimeSpan ServiceTimeout { get; init; } = TimeSpan.FromSeconds(120);

    public void Validate()
    {
        ValidatePositive(BridgeTimeout, nameof(BridgeTimeout));
        ValidatePositive(ReceiptTimeout, nameof(ReceiptTimeout));
        ValidatePositive(WindowsTimeout, nameof(WindowsTimeout));
        ValidatePositive(ServiceTimeout, nameof(ServiceTimeout));
    }

    private static void ValidatePositive(TimeSpan value, string propertyName)
    {
        if (value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(propertyName, value, "Timeout must be positive.");
        }
    }
}

