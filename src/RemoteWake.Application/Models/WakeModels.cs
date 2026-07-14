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

public sealed record VpnStatus(bool IsConnected);

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
