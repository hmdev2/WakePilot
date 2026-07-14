using RemoteWake.Domain.Results;
using RemoteWake.Domain.Wake;

namespace RemoteWake.Domain.Tests;

[TestClass]
public sealed class WakeStateMachineTests
{
    [TestMethod]
    public void WakePathPreservesEveryObservablePhase()
    {
        var machine = new WakeStateMachine();

        Move(machine, WakeTrigger.WakeRequired, new(false, true));
        Assert.AreEqual(WakeState.SendingWake, machine.Current);

        Move(machine, WakeTrigger.WakeAccepted);
        Assert.AreEqual(WakeState.WaitingWindows, machine.Current, "A receipt must not imply Windows readiness.");

        Move(machine, WakeTrigger.WindowsReady);
        Move(machine, WakeTrigger.ServiceReady);
        Move(machine, WakeTrigger.ClientOpened, new(LaunchSucceeded: true));

        Assert.AreEqual(WakeState.Completed, machine.Current);
    }

    [TestMethod]
    public void AlreadyReadyPathCanOpenImmediatelyOrWaitForService()
    {
        var immediate = new WakeStateMachine();
        Move(immediate, WakeTrigger.PcAlreadyReady);
        Move(immediate, WakeTrigger.ServiceReady);
        Move(immediate, WakeTrigger.ClientOpened, new(LaunchSucceeded: true));
        Assert.AreEqual(WakeState.Completed, immediate.Current);

        var pending = new WakeStateMachine();
        Move(pending, WakeTrigger.PcAlreadyReady);
        Move(pending, WakeTrigger.ServicePending);
        Move(pending, WakeTrigger.ServiceReady);
        Assert.AreEqual(WakeState.OpeningClient, pending.Current);
    }

    [TestMethod]
    public void BridgeUnavailableIsObservableBeforeFailure()
    {
        var machine = new WakeStateMachine();

        Move(machine, WakeTrigger.BridgeUnavailable);
        Assert.AreEqual(WakeState.BridgeUnavailable, machine.Current);

        Move(machine, WakeTrigger.Fail);
        Assert.AreEqual(WakeState.Failed, machine.Current);
    }

    [TestMethod]
    public void SendingWakeRequiresOfflinePcAndHealthyAuthenticatedBridge()
    {
        var pcReady = new WakeStateMachine();
        var readyResult = pcReady.TryTransition(WakeTrigger.WakeRequired, new(true, true));

        var bridgeUnhealthy = new WakeStateMachine();
        var bridgeResult = bridgeUnhealthy.TryTransition(WakeTrigger.WakeRequired, new(false, false));

        Assert.IsTrue(readyResult.IsFailure);
        Assert.AreEqual(ErrorCode.ERR020, readyResult.Error!.Code);
        Assert.AreEqual(WakeState.Checking, pcReady.Current);
        Assert.IsTrue(bridgeResult.IsFailure);
        Assert.AreEqual(WakeState.Checking, bridgeUnhealthy.Current);
    }

    [TestMethod]
    public void CompletedRequiresSuccessfulLaunchResult()
    {
        var machine = ReachOpeningClient();

        var rejected = machine.TryTransition(WakeTrigger.ClientOpened);

        Assert.IsTrue(rejected.IsFailure);
        Assert.AreEqual(WakeState.OpeningClient, machine.Current);

        Move(machine, WakeTrigger.ClientOpened, new(LaunchSucceeded: true));
        Assert.AreEqual(WakeState.Completed, machine.Current);
    }

    [TestMethod]
    public void CancellationIsTerminalAndDisablesRetry()
    {
        var machine = new WakeStateMachine();
        Move(machine, WakeTrigger.WakeRequired, new(false, true));
        Move(machine, WakeTrigger.WakeAccepted);

        Move(machine, WakeTrigger.Cancel);

        Assert.AreEqual(WakeState.Cancelled, machine.Current);
        Assert.IsFalse(machine.CanRetry);
        var retry = machine.TryTransition(WakeTrigger.WakeRequired, new(false, true));
        Assert.IsTrue(retry.IsFailure);
    }

    [TestMethod]
    public void InvalidOrTerminalTransitionsAreRejectedWithoutChangingState()
    {
        var invalid = new WakeStateMachine();
        var result = invalid.TryTransition(WakeTrigger.WindowsReady);
        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(WakeState.Checking, invalid.Current);

        Move(invalid, WakeTrigger.Fail);
        var terminal = invalid.TryTransition(WakeTrigger.Cancel);
        Assert.IsTrue(terminal.IsFailure);
        Assert.AreEqual(WakeState.Failed, invalid.Current);
    }

    private static WakeStateMachine ReachOpeningClient()
    {
        var machine = new WakeStateMachine();
        Move(machine, WakeTrigger.PcAlreadyReady);
        Move(machine, WakeTrigger.ServiceReady);
        return machine;
    }

    private static void Move(
        WakeStateMachine machine,
        WakeTrigger trigger,
        WakeTransitionContext? context = null)
    {
        var result = machine.TryTransition(trigger, context);
        Assert.IsTrue(result.IsSuccess, result.Error?.Reason);
    }
}
