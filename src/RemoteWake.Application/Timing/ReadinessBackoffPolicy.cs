using RemoteWake.Application.Ports;

namespace RemoteWake.Application.Timing;

public sealed class ReadinessBackoffPolicy : IBackoffPolicy
{
    private static readonly TimeSpan[] Delays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8),
        TimeSpan.FromSeconds(10),
    ];

    public TimeSpan GetDelay(int attempt)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(attempt);

        return Delays[Math.Min(attempt, Delays.Length - 1)];
    }
}
