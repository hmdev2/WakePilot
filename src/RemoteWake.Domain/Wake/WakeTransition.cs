namespace RemoteWake.Domain.Wake;

public sealed record WakeTransitionContext(
    bool PcIsReady = false,
    bool BridgeIsAuthenticatedAndHealthy = false,
    bool LaunchSucceeded = false);

public sealed record WakeTransition(WakeState Previous, WakeState Current, WakeTrigger Trigger);
