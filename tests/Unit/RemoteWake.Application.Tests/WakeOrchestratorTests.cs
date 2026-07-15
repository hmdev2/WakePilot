using RemoteWake.Application.Models;
using RemoteWake.Application.Orchestration;
using RemoteWake.Application.Tests.Fakes;
using RemoteWake.Application.Timing;
using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Results;
using RemoteWake.Domain.Wake;

namespace RemoteWake.Application.Tests;

[TestClass]
public sealed class WakeOrchestratorTests
{
    [TestMethod]
    public async Task FullFakeFlowCompletesAcrossAllRequiredPhases()
    {
        var fixture = new Fixture();
        var progress = new RecordingProgress();
        fixture.Probe.EnqueueWindows(
            Result.Success(new WindowsReadiness(false)),
            Result.Success(new WindowsReadiness(false)),
            Result.Success(new WindowsReadiness(true)));
        fixture.Probe.EnqueueServices(Result.Success(new RemoteServiceReadiness(true)));

        var result = await fixture.CreateOrchestrator().ExecuteAsync(
            fixture.Profile,
            progress,
            CancellationToken.None);

        CollectionAssert.AreEqual(
            new[]
            {
                WakeState.Checking,
                WakeState.SendingWake,
                WakeState.WaitingWindows,
                WakeState.WaitingService,
                WakeState.OpeningClient,
                WakeState.Completed,
            },
            result.StateHistory.ToArray());
        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, fixture.Bridge.SentCommands);
        Assert.AreEqual(fixture.Profile.TargetId, fixture.Bridge.SentCommands[0].TargetId);
        Assert.AreEqual("[REDACTED]", fixture.Bridge.SentCommands[0].Nonce.ToString());
        Assert.AreEqual("service", fixture.Probe.Calls[^1]);
        CollectionAssert.AreEqual(
            new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1) },
            fixture.Clock.Delays);
        Assert.AreEqual(1, fixture.RemoteApp.Calls);
        CollectionAssert.AreEqual(result.StateHistory.ToArray(), progress.Updates.Select(update => update.State).ToArray());
        Assert.IsTrue(progress.Updates.All(update => update.CorrelationId == result.CorrelationId));
    }

    [TestMethod]
    public async Task AlreadyReadyDoesNotContactVpnOrBridge()
    {
        var fixture = new Fixture();
        fixture.Probe.EnqueueWindows(Result.Success(new WindowsReadiness(true)));
        fixture.Probe.EnqueueServices(Result.Success(new RemoteServiceReadiness(true)));

        var result = await fixture.CreateOrchestrator().ExecuteAsync(fixture.Profile, CancellationToken.None);

        CollectionAssert.AreEqual(
            new[] { WakeState.Checking, WakeState.AlreadyReady, WakeState.OpeningClient, WakeState.Completed },
            result.StateHistory.ToArray());
        Assert.AreEqual(0, fixture.Vpn.Calls);
        Assert.AreEqual(0, fixture.Bridge.HealthCalls);
        Assert.IsEmpty(fixture.Bridge.SentCommands);
    }

    [TestMethod]
    public async Task ReadyWindowsWithPendingServiceWaitsWithoutSendingWake()
    {
        var fixture = new Fixture();
        fixture.Probe.EnqueueWindows(Result.Success(new WindowsReadiness(true)));
        fixture.Probe.EnqueueServices(
            Result.Success(new RemoteServiceReadiness(false)),
            Result.Success(new RemoteServiceReadiness(true)));

        var result = await fixture.CreateOrchestrator().ExecuteAsync(fixture.Profile, CancellationToken.None);

        CollectionAssert.AreEqual(
            new[]
            {
                WakeState.Checking,
                WakeState.AlreadyReady,
                WakeState.WaitingService,
                WakeState.OpeningClient,
                WakeState.Completed,
            },
            result.StateHistory.ToArray());
        Assert.IsEmpty(fixture.Bridge.SentCommands);
        Assert.HasCount(1, fixture.Clock.Delays);
    }

    [TestMethod]
    public async Task DisconnectedVpnReportsBridgeUnavailableWithoutWake()
    {
        var fixture = new Fixture();
        fixture.Probe.EnqueueWindows(Result.Success(new WindowsReadiness(false)));
        fixture.Vpn.Status = Result.Success(new VpnStatus(false));

        var result = await fixture.CreateOrchestrator().ExecuteAsync(fixture.Profile, CancellationToken.None);

        CollectionAssert.AreEqual(
            new[] { WakeState.Checking, WakeState.BridgeUnavailable, WakeState.Failed },
            result.StateHistory.ToArray());
        Assert.AreEqual(ErrorCode.ERR008, result.Error!.Code);
        Assert.IsEmpty(fixture.Bridge.SentCommands);
    }

    [TestMethod]
    public async Task UnhealthyBridgeIsRejectedBeforeWake()
    {
        var fixture = new Fixture();
        fixture.Probe.EnqueueWindows(Result.Success(new WindowsReadiness(false)));
        fixture.Bridge.Health = Result.Success(new BridgeHealth(true, false));

        var result = await fixture.CreateOrchestrator().ExecuteAsync(fixture.Profile, CancellationToken.None);

        Assert.AreEqual(ErrorCode.ERR009, result.Error!.Code);
        Assert.IsEmpty(fixture.Bridge.SentCommands);
    }

    [TestMethod]
    public async Task RejectedWakeReceiptNeverClaimsWindowsReady()
    {
        var fixture = new Fixture();
        fixture.Probe.EnqueueWindows(Result.Success(new WindowsReadiness(false)));
        fixture.Bridge.ReceiptFactory = command =>
            Result.Success(new WakeReceipt(command.RequestId, false, 0));

        var result = await fixture.CreateOrchestrator().ExecuteAsync(fixture.Profile, CancellationToken.None);

        Assert.AreEqual(ErrorCode.ERR012, result.Error!.Code);
        Assert.IsFalse(result.StateHistory.Contains(WakeState.WaitingWindows));
        Assert.IsFalse(result.StateHistory.Contains(WakeState.WaitingService));
    }

    [TestMethod]
    public async Task CancellationDuringProbeDelayStopsWithoutRetryOrLaunch()
    {
        var fixture = new Fixture();
        using var cancellation = new CancellationTokenSource();
        fixture.Probe.EnqueueWindows(Result.Success(new WindowsReadiness(false)));
        fixture.Clock.BeforeDelayCompletion = cancellation.Cancel;

        var result = await fixture.CreateOrchestrator().ExecuteAsync(fixture.Profile, cancellation.Token);

        Assert.AreEqual(WakeState.Cancelled, result.FinalState);
        Assert.AreEqual(WakeState.Cancelled, result.StateHistory[^1]);
        Assert.HasCount(1, fixture.Bridge.SentCommands);
        Assert.AreEqual(0, fixture.RemoteApp.Calls);
    }

    [TestMethod]
    public async Task WindowsTimeoutFailsWithErr014()
    {
        var fixture = new Fixture();
        fixture.Probe.EnqueueWindows(Result.Success(new WindowsReadiness(false)));
        var options = new WakeOrchestratorOptions
        {
            WindowsTimeout = TimeSpan.FromSeconds(3),
        };

        var result = await fixture.CreateOrchestrator(options).ExecuteAsync(fixture.Profile, CancellationToken.None);

        Assert.AreEqual(WakeState.Failed, result.FinalState);
        Assert.AreEqual(ErrorCode.ERR014, result.Error!.Code);
        CollectionAssert.AreEqual(
            new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) },
            fixture.Clock.Delays);
    }

    [TestMethod]
    public async Task ServiceTimeoutStartsOnlyAfterWindowsReady()
    {
        var fixture = new Fixture();
        fixture.Probe.EnqueueWindows(Result.Success(new WindowsReadiness(true)));
        fixture.Probe.EnqueueServices(Result.Success(new RemoteServiceReadiness(false)));
        var options = new WakeOrchestratorOptions
        {
            ServiceTimeout = TimeSpan.FromSeconds(3),
        };

        var result = await fixture.CreateOrchestrator(options).ExecuteAsync(fixture.Profile, CancellationToken.None);

        Assert.AreEqual(ErrorCode.ERR015, result.Error!.Code);
        Assert.AreEqual("windows", fixture.Probe.Calls[0]);
        Assert.IsTrue(fixture.Probe.Calls.Skip(1).All(call => call == "service"));
    }

    [TestMethod]
    public async Task LaunchFailurePreventsCompletedState()
    {
        var fixture = new Fixture();
        fixture.Probe.EnqueueWindows(Result.Success(new WindowsReadiness(true)));
        fixture.Probe.EnqueueServices(Result.Success(new RemoteServiceReadiness(true)));
        fixture.RemoteApp.LaunchResult = Result.Success(new LaunchResult(false));

        var result = await fixture.CreateOrchestrator().ExecuteAsync(fixture.Profile, CancellationToken.None);

        Assert.AreEqual(ErrorCode.ERR016, result.Error!.Code);
        Assert.AreEqual(WakeState.Failed, result.FinalState);
        Assert.IsFalse(result.StateHistory.Contains(WakeState.Completed));
    }

    [TestMethod]
    public async Task NonRetryableProbeFailureStopsImmediately()
    {
        var fixture = new Fixture();
        fixture.Probe.EnqueueWindows(
            Result.Success(new WindowsReadiness(false)),
            Result.Failure<WindowsReadiness>(DomainError.Create(ErrorCode.ERR021, "probe_invalid")));

        var result = await fixture.CreateOrchestrator().ExecuteAsync(fixture.Profile, CancellationToken.None);

        Assert.AreEqual(ErrorCode.ERR021, result.Error!.Code);
        Assert.HasCount(1, fixture.Clock.Delays);
    }

    private sealed class Fixture
    {
        public FakeVpnAdapter Vpn { get; } = new();

        public FakeBridgeClient Bridge { get; } = new();

        public FakeWakeStateProbe Probe { get; } = new();

        public FakeRemoteAppAdapter RemoteApp { get; } = new();

        public FakeAuditSink Audit { get; } = new();

        public FakeClock Clock { get; } = new();

        public WakeProfile Profile { get; } =
            new(ComputerId.New(), BridgeId.New(), TargetId.New(), "rustdesk");

        public WakeOrchestrator CreateOrchestrator(WakeOrchestratorOptions? options = null) =>
            new(
                Vpn,
                Bridge,
                Probe,
                RemoteApp,
                Audit,
                Clock,
                new FakeNonceGenerator(),
                new ReadinessBackoffPolicy(),
                options);
    }

    private sealed class RecordingProgress : IProgress<WakeProgressUpdate>
    {
        public List<WakeProgressUpdate> Updates { get; } = [];

        public void Report(WakeProgressUpdate value) => Updates.Add(value);
    }
}
