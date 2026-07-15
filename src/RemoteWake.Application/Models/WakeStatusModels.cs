using RemoteWake.Domain.Results;

namespace RemoteWake.Application.Models;

public enum ComputerOperationalState
{
    Ready,
    NotReady,
    Unknown,
}

public enum BridgeOperationalState
{
    Ready,
    VpnDisconnected,
    Unavailable,
    IdentityMismatch,
    Unknown,
}

public enum RemoteApplicationOperationalState
{
    Ready,
    NotReady,
    Unknown,
}

public sealed record WakeStatusSnapshot(
    ComputerOperationalState Computer,
    BridgeOperationalState Bridge,
    RemoteApplicationOperationalState RemoteApplication,
    DateTimeOffset ObservedAt,
    ErrorCode? ComputerError = null,
    ErrorCode? BridgeError = null,
    ErrorCode? RemoteApplicationError = null)
{
    public bool CanStart =>
        Computer == ComputerOperationalState.Ready || Bridge == BridgeOperationalState.Ready;
}
