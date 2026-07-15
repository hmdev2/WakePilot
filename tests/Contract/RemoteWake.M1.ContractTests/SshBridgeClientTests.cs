using RemoteWake.Application.Models;
using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Results;
using RemoteWake.Domain.Security;
using RemoteWake.Infrastructure.SshBridge;

namespace RemoteWake.M1.ContractTests;

[TestClass]
public sealed class SshBridgeClientTests
{
    private static readonly BridgeId BridgeId = BridgeId.From(Guid.Parse("11111111-1111-4111-8111-111111111111"));
    private static readonly TargetId TargetId = TargetId.From(Guid.Parse("22222222-2222-4222-8222-222222222222"));
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task Ct012HostIdentityMismatchBlocksBridge()
    {
        var runner = new RecordingProcessRunner();
        runner.Enqueue(new ProcessExecutionResult(
            255,
            string.Empty,
            "REMOTE HOST IDENTIFICATION HAS CHANGED"));
        var client = CreateClient(runner, out _);

        var result = await client.GetHealthAsync(BridgeId, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ErrorCode.ERR010, result.Error!.Code);
    }

    [TestMethod]
    public async Task Ct017AuthorizedWakeRequiresThreePacketReceipt()
    {
        var runner = new RecordingProcessRunner();
        var requestId = RequestId.From(Guid.Parse("44444444-4444-4444-8444-444444444444"));
        runner.Enqueue(new ProcessExecutionResult(
            0,
            BridgeProtocolTests.ResponseJson(requestId, "accepted", "OK", 3) + Environment.NewLine,
            string.Empty));
        var client = CreateClient(runner, out var leaseProvider);
        var command = new WakeCommand(requestId, TargetId, Now, Nonce.Parse(new string('B', 43)));

        var result = await client.SendWakeAsync(BridgeId, command, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Value.IsAccepted);
        Assert.AreEqual(3, result.Value.PacketCount);
        Assert.AreEqual(1, leaseProvider.AcquireCount);
        Assert.IsTrue(leaseProvider.IsDisposed);
        var invocation = runner.Invocations.Single();
        AssertSafeSshArguments(invocation.Arguments);
        var decoded = BridgeProtocolCodec.DecodeRequest(invocation.Arguments[^1]);
        Assert.AreEqual(BridgeAction.Wake, decoded.Action);
        Assert.AreEqual(TargetId, decoded.TargetId);
    }

    [TestMethod]
    public async Task Ct013NonAllowlistedTargetNeverStartsSsh()
    {
        var runner = new RecordingProcessRunner();
        var client = CreateClient(runner, out _);
        var command = new WakeCommand(
            RequestId.New(),
            TargetId.New(),
            Now,
            Nonce.Parse(new string('C', 43)));

        var result = await client.SendWakeAsync(BridgeId, command, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ErrorCode.ERR012, result.Error!.Code);
        Assert.AreEqual(0, runner.Invocations.Count);
    }

    [TestMethod]
    public async Task Ct018StaleOrMalformedAcceptedReceiptIsRejected()
    {
        var requestId = RequestId.From(Guid.Parse("55555555-5555-4555-8555-555555555555"));
        var staleRunner = new RecordingProcessRunner();
        staleRunner.Enqueue(new ProcessExecutionResult(
            0,
            BridgeProtocolTests.ResponseJson(requestId, "accepted", "OK", 3)
                .Replace("2026-07-14T12:00:00.0000000Z", "2026-07-14T11:58:00.0000000Z"),
            string.Empty));
        var stale = await CreateClient(staleRunner, out _).SendWakeAsync(
            BridgeId,
            new WakeCommand(requestId, TargetId, Now, Nonce.Parse(new string('E', 43))),
            CancellationToken.None);
        Assert.IsTrue(stale.IsFailure);
        Assert.AreEqual(ErrorCode.ERR011, stale.Error!.Code);

        var countRunner = new RecordingProcessRunner();
        countRunner.Enqueue(new ProcessExecutionResult(
            0,
            BridgeProtocolTests.ResponseJson(requestId, "accepted", "OK", 2),
            string.Empty));
        var count = await CreateClient(countRunner, out _).SendWakeAsync(
            BridgeId,
            new WakeCommand(requestId, TargetId, Now, Nonce.Parse(new string('F', 43))),
            CancellationToken.None);
        Assert.IsTrue(count.IsFailure);
        Assert.AreEqual(ErrorCode.ERR012, count.Error!.Code);
    }

    [TestMethod]
    public void Ct010AndCt011SshInvocationDisablesShellPtyForwardingAndFallbackAuth()
    {
        var client = CreateClient(new RecordingProcessRunner(), out _);
        var endpoint = CreateEndpoint();
        var request = new BridgeProtocolRequest(
            RequestId.New(),
            TargetId,
            Now,
            Nonce.Parse(new string('D', 43)),
            BridgeAction.Health);

        var identityPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "leased-identity"));
        var invocation = client.CreateInvocation(endpoint, request, identityPath);

        AssertSafeSshArguments(invocation.Arguments);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(Path.GetTempPath(), "ssh.exe")), invocation.ExecutablePath);
    }

    private static SshBridgeClient CreateClient(
        RecordingProcessRunner runner,
        out RecordingPrivateKeyLeaseProvider leaseProvider)
    {
        var options = new SshBridgeOptions(
            Path.GetFullPath(Path.Combine(Path.GetTempPath(), "ssh.exe")),
            Path.GetFullPath(Path.Combine(Path.GetTempPath(), "known_hosts")),
            new Dictionary<BridgeId, SshBridgeEndpoint> { [BridgeId] = CreateEndpoint() });
        leaseProvider = new RecordingPrivateKeyLeaseProvider(
            Path.GetFullPath(Path.Combine(Path.GetTempPath(), "leased-identity")));
        return new SshBridgeClient(
            runner,
            leaseProvider,
            options,
            new FixedClock(Now),
            new FixedNonceGenerator(new string('A', 43)));
    }

    private static SshBridgeEndpoint CreateEndpoint() =>
        new("100.64.0.10", 8022, "u0_a123", TargetId);

    private static void AssertSafeSshArguments(IReadOnlyList<string> arguments)
    {
        var joined = string.Join('\n', arguments);
        foreach (var required in new[]
        {
            "BatchMode=yes",
            "StrictHostKeyChecking=yes",
            "GlobalKnownHostsFile=none",
            "IdentitiesOnly=yes",
            "IdentityAgent=none",
            "PasswordAuthentication=no",
            "KbdInteractiveAuthentication=no",
            "ForwardAgent=no",
            "ForwardX11=no",
            "ClearAllForwardings=yes",
            "PermitLocalCommand=no",
            "RequestTTY=no",
            "ControlMaster=no",
        })
        {
            StringAssert.Contains(joined, required);
        }

        CollectionAssert.Contains(arguments.ToArray(), "-T");
        CollectionAssert.Contains(arguments.ToArray(), "-n");
        CollectionAssert.DoesNotContain(arguments.ToArray(), "-t");
        CollectionAssert.DoesNotContain(arguments.ToArray(), "sh");
        CollectionAssert.DoesNotContain(arguments.ToArray(), "cmd.exe");
        CollectionAssert.DoesNotContain(arguments.ToArray(), "powershell.exe");
    }
}
