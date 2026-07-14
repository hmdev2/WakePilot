namespace RemoteWake.Domain.Wake;

public enum WakeState
{
    Checking,
    AlreadyReady,
    BridgeUnavailable,
    SendingWake,
    WaitingWindows,
    WaitingService,
    OpeningClient,
    Completed,
    Failed,
    Cancelled,
}

public enum WakeTrigger
{
    PcAlreadyReady,
    BridgeUnavailable,
    WakeRequired,
    WakeAccepted,
    WindowsReady,
    ServicePending,
    ServiceReady,
    ClientOpened,
    Fail,
    Cancel,
}

