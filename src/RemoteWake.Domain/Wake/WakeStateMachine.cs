using RemoteWake.Domain.Results;

namespace RemoteWake.Domain.Wake;

public sealed class WakeStateMachine
{
    private static readonly Dictionary<(WakeState State, WakeTrigger Trigger), WakeState> Transitions =
        new Dictionary<(WakeState, WakeTrigger), WakeState>
        {
            [(WakeState.Checking, WakeTrigger.PcAlreadyReady)] = WakeState.AlreadyReady,
            [(WakeState.Checking, WakeTrigger.BridgeUnavailable)] = WakeState.BridgeUnavailable,
            [(WakeState.Checking, WakeTrigger.WakeRequired)] = WakeState.SendingWake,
            [(WakeState.AlreadyReady, WakeTrigger.ServicePending)] = WakeState.WaitingService,
            [(WakeState.AlreadyReady, WakeTrigger.ServiceReady)] = WakeState.OpeningClient,
            [(WakeState.SendingWake, WakeTrigger.WakeAccepted)] = WakeState.WaitingWindows,
            [(WakeState.WaitingWindows, WakeTrigger.WindowsReady)] = WakeState.WaitingService,
            [(WakeState.WaitingService, WakeTrigger.ServiceReady)] = WakeState.OpeningClient,
            [(WakeState.OpeningClient, WakeTrigger.ClientOpened)] = WakeState.Completed,
        };

    public WakeState Current { get; private set; } = WakeState.Checking;

    public bool CanRetry { get; private set; } = true;

    public Result<WakeTransition> TryTransition(
        WakeTrigger trigger,
        WakeTransitionContext? context = null)
    {
        context ??= new WakeTransitionContext();

        if (IsTerminal(Current))
        {
            return InvalidTransition(trigger, "terminal_state");
        }

        if (trigger == WakeTrigger.Cancel)
        {
            return MoveTo(WakeState.Cancelled, trigger);
        }

        if (trigger == WakeTrigger.Fail)
        {
            return MoveTo(WakeState.Failed, trigger);
        }

        if (!Transitions.TryGetValue((Current, trigger), out var target))
        {
            return InvalidTransition(trigger, "transition_not_allowed");
        }

        if (target == WakeState.SendingWake &&
            (context.PcIsReady || !context.BridgeIsAuthenticatedAndHealthy))
        {
            return InvalidTransition(trigger, "sending_wake_invariant_failed");
        }

        if (target == WakeState.Completed && !context.LaunchSucceeded)
        {
            return InvalidTransition(trigger, "launch_result_required");
        }

        return MoveTo(target, trigger);
    }

    private static bool IsTerminal(WakeState state) =>
        state is WakeState.Completed or WakeState.Failed or WakeState.Cancelled;

    private Result<WakeTransition> MoveTo(WakeState target, WakeTrigger trigger)
    {
        var previous = Current;
        Current = target;

        if (target == WakeState.Cancelled)
        {
            CanRetry = false;
        }

        return Result.Success(new WakeTransition(previous, target, trigger));
    }

    private Result<WakeTransition> InvalidTransition(WakeTrigger trigger, string reason) =>
        Result.Failure<WakeTransition>(
            DomainError.Create(ErrorCode.ERR020, $"{reason}:{Current}:{trigger}"));
}
