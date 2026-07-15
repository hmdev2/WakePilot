using RemoteWake.Application.Models;
using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Results;
using RemoteWake.Infrastructure.Tailscale;

namespace RemoteWake.M1.ContractTests;

[TestClass]
public sealed class TailscaleAdapterTests
{
    private static readonly BridgeId BridgeId = BridgeId.From(Guid.Parse("11111111-1111-4111-8111-111111111111"));
    private static readonly string Executable = Path.GetFullPath(
        Path.Combine(Path.GetTempPath(), "tailscale.exe"));

    [TestMethod]
    public async Task Ct016RunningVpnResolvesConfiguredOnlineBridgeIpv4()
    {
        var runner = new RecordingProcessRunner();
        runner.Enqueue(new ProcessExecutionResult(
            0,
            """
            {"BackendState":"Running","Peer":{"node-key:fixture":{"HostName":"android-bridge","DNSName":"android-bridge.example.ts.net.","TailscaleIPs":["100.64.0.10","fd7a:115c:a1e0::10"],"Online":true}}}
            """,
            string.Empty));
        var adapter = CreateAdapter(runner, "android-bridge.example.ts.net");

        var result = await adapter.GetStatusAsync(BridgeId, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Value.IsConnected);
        Assert.IsTrue(result.Value.IsPeerOnline);
        Assert.AreEqual("100.64.0.10", result.Value.PeerAddress);
        var arguments = runner.Invocations.Single().Arguments;
        Assert.AreEqual(2, arguments.Count);
        Assert.AreEqual("status", arguments[0]);
        Assert.AreEqual("--json", arguments[1]);
    }

    [TestMethod]
    public async Task Ct016DisconnectedAndCommandFailureRemainDistinctTypedResults()
    {
        var disconnectedRunner = new RecordingProcessRunner();
        disconnectedRunner.Enqueue(new ProcessExecutionResult(0, "{\"BackendState\":\"NeedsLogin\"}", string.Empty));
        var disconnected = await CreateAdapter(disconnectedRunner, "android-bridge")
            .GetStatusAsync(BridgeId, CancellationToken.None);
        Assert.IsTrue(disconnected.IsSuccess);
        Assert.IsFalse(disconnected.Value.IsConnected);

        var failedRunner = new RecordingProcessRunner();
        failedRunner.Enqueue(new ProcessExecutionResult(1, string.Empty, "not logged in"));
        var failed = await CreateAdapter(failedRunner, "android-bridge")
            .GetStatusAsync(BridgeId, CancellationToken.None);
        Assert.IsTrue(failed.IsFailure);
        Assert.AreEqual(ErrorCode.ERR008, failed.Error!.Code);
    }

    private static TailscaleVpnAdapter CreateAdapter(RecordingProcessRunner runner, string nodeName) =>
        new(
            runner,
            new TailscaleOptions(
                Executable,
                new Dictionary<BridgeId, string> { [BridgeId] = nodeName }));
}
