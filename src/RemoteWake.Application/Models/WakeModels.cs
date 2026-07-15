using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Results;
using RemoteWake.Domain.Security;
using RemoteWake.Domain.Wake;

namespace RemoteWake.Application.Models;

public sealed record WakeProfile
{
    public WakeProfile(
        ComputerId computerId,
        BridgeId bridgeId,
        TargetId targetId,
        string requestedServiceId)
    {
        ComputerId = computerId ?? throw new ArgumentNullException(nameof(computerId));
        BridgeId = bridgeId ?? throw new ArgumentNullException(nameof(bridgeId));
        TargetId = targetId ?? throw new ArgumentNullException(nameof(targetId));
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedServiceId);
        RequestedServiceId = requestedServiceId;
    }

    public ComputerId ComputerId { get; }

    public BridgeId BridgeId { get; }

    public TargetId TargetId { get; }

    public string RequestedServiceId { get; }
}

public sealed record VpnStatus(
    bool IsConnected,
    string? PeerAddress = null,
    bool IsPeerOnline = false);

public sealed record BridgeHealth(bool IsAuthenticated, bool IsHealthy)
{
    public bool CanWake => IsAuthenticated && IsHealthy;
}

public sealed record WakeCommand(
    RequestId RequestId,
    TargetId TargetId,
    DateTimeOffset IssuedAt,
    Nonce Nonce);

public sealed record WakeReceipt(RequestId RequestId, bool IsAccepted, int PacketCount);

public sealed record WindowsReadiness(bool IsReady);

public sealed record RemoteServiceReadiness(bool IsReady);

public sealed record LaunchResult(bool Started);

public sealed record WakeExecutionResult(
    WakeState FinalState,
    IReadOnlyList<WakeState> StateHistory,
    DomainError? Error,
    CorrelationId CorrelationId,
    TimeSpan Duration)
{
    public bool IsSuccess => FinalState == WakeState.Completed;
}

public sealed record WindowsDiagnosticSnapshot(DateTimeOffset ObservedAt, string EvidenceStatus);

public sealed record SecretReference(string Name);

public sealed record ComputerProfileSnapshot(ComputerId ComputerId, string DisplayName);

public sealed record WakeAttemptSnapshot(
    RequestId RequestId,
    ComputerId ComputerId,
    WakeState State,
    DateTimeOffset ObservedAt);

public enum PrivilegedOperation
{
    EnableWakeOnLan,
    ConfigureReadinessFirewall,
    InstallReadinessAgent,
    RemoveReadinessAgent,
}

public sealed record PrivilegedOperationRequest(PrivilegedOperation Operation, ComputerId ComputerId);

public sealed record PrivilegedOperationResult(bool Applied, bool Verified);

public sealed record ProcessInvocation
{
    public ProcessInvocation(
        string executablePath,
        IEnumerable<string> arguments,
        TimeSpan timeout,
        int maximumOutputCharacters = 16_384,
        bool completeOnFirstOutputLine = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(arguments);

        if (!Path.IsPathFullyQualified(executablePath))
        {
            throw new ArgumentException("Executable path must be absolute.", nameof(executablePath));
        }

        var fullPath = Path.GetFullPath(executablePath);
        if (!string.Equals(fullPath, executablePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Executable path must be canonical.", nameof(executablePath));
        }

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive.");
        }

        if (maximumOutputCharacters is < 1 or > 65_536)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumOutputCharacters),
                "Output limit must be between 1 and 65536 characters.");
        }

        ExecutablePath = fullPath;
        Arguments = arguments.Select(argument =>
        {
            ArgumentNullException.ThrowIfNull(argument);
            return argument;
        }).ToArray();
        Timeout = timeout;
        MaximumOutputCharacters = maximumOutputCharacters;
        CompleteOnFirstOutputLine = completeOnFirstOutputLine;
    }

    public string ExecutablePath { get; }

    public IReadOnlyList<string> Arguments { get; }

    public TimeSpan Timeout { get; }

    public int MaximumOutputCharacters { get; }

    public bool CompleteOnFirstOutputLine { get; }
}

public sealed record ProcessExecutionResult(int ExitCode, string StandardOutput, string StandardError);
