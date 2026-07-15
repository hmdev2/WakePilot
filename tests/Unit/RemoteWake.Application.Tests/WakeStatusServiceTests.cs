using RemoteWake.Application.Models;
using RemoteWake.Application.Status;
using RemoteWake.Application.Tests.Fakes;
using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Results;

namespace RemoteWake.Application.Tests;

[TestClass]
public sealed class WakeStatusServiceTests
{
    [TestMethod]
    public async Task NotReadyComputerWithHealthyBridgeCanStartWithoutServiceProbe()
    {
        var fixture = new Fixture();
        fixture.Probe.EnqueueWindows(Result.Success(new WindowsReadiness(false)));

        var status = await fixture.Service.RefreshAsync(fixture.Profile, CancellationToken.None);

        Assert.AreEqual(ComputerOperationalState.NotReady, status.Computer);
        Assert.AreEqual(BridgeOperationalState.Ready, status.Bridge);
        Assert.AreEqual(RemoteApplicationOperationalState.Unknown, status.RemoteApplication);
        Assert.IsTrue(status.CanStart);
        Assert.HasCount(1, fixture.Probe.Calls);
        Assert.AreEqual("windows", fixture.Probe.Calls[0]);
    }

    [TestMethod]
    public async Task ReadyComputerCanOpenWhenVpnIsDisconnected()
    {
        var fixture = new Fixture();
        fixture.Probe.EnqueueWindows(Result.Success(new WindowsReadiness(true)));
        fixture.Probe.EnqueueServices(Result.Success(new RemoteServiceReadiness(true)));
        fixture.Vpn.Status = Result.Success(new VpnStatus(false));

        var status = await fixture.Service.RefreshAsync(fixture.Profile, CancellationToken.None);

        Assert.AreEqual(ComputerOperationalState.Ready, status.Computer);
        Assert.AreEqual(RemoteApplicationOperationalState.Ready, status.RemoteApplication);
        Assert.AreEqual(BridgeOperationalState.VpnDisconnected, status.Bridge);
        Assert.AreEqual(ErrorCode.ERR008, status.BridgeError);
        Assert.IsTrue(status.CanStart);
        Assert.AreEqual(0, fixture.Bridge.HealthCalls);
    }

    [TestMethod]
    public async Task ChangedBridgeIdentityHasDistinctBlockedState()
    {
        var fixture = new Fixture();
        fixture.Probe.EnqueueWindows(Result.Success(new WindowsReadiness(false)));
        fixture.Bridge.Health = Result.Failure<BridgeHealth>(
            DomainError.Create(ErrorCode.ERR010, "host_key_mismatch"));

        var status = await fixture.Service.RefreshAsync(fixture.Profile, CancellationToken.None);

        Assert.AreEqual(BridgeOperationalState.IdentityMismatch, status.Bridge);
        Assert.AreEqual(ErrorCode.ERR010, status.BridgeError);
        Assert.IsFalse(status.CanStart);
    }

    [TestMethod]
    public async Task FailedWindowsProbeNeverClaimsComputerOrApplicationReady()
    {
        var fixture = new Fixture();
        fixture.Probe.EnqueueWindows(Result.Failure<WindowsReadiness>(
            DomainError.Create(ErrorCode.ERR021, "readiness_unavailable", true)));

        var status = await fixture.Service.RefreshAsync(fixture.Profile, CancellationToken.None);

        Assert.AreEqual(ComputerOperationalState.Unknown, status.Computer);
        Assert.AreEqual(RemoteApplicationOperationalState.Unknown, status.RemoteApplication);
        Assert.AreEqual(ErrorCode.ERR021, status.ComputerError);
        Assert.IsTrue(status.CanStart);
    }

    private sealed class Fixture
    {
        public Fixture()
        {
            Service = new WakeStatusService(Vpn, Bridge, Probe, Clock);
        }

        public FakeVpnAdapter Vpn { get; } = new();

        public FakeBridgeClient Bridge { get; } = new();

        public FakeWakeStateProbe Probe { get; } = new();

        public FakeClock Clock { get; } = new();

        public WakeProfile Profile { get; } =
            new(ComputerId.New(), BridgeId.New(), TargetId.New(), "rustdesk");

        public WakeStatusService Service { get; }
    }
}
