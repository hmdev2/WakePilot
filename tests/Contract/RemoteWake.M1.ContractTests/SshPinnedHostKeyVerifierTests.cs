using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Results;
using RemoteWake.Infrastructure.SshBridge;

namespace RemoteWake.M1.ContractTests;

[TestClass]
public sealed class SshPinnedHostKeyVerifierTests
{
    private const string EncodedKey =
        "AAAAC3NzaC1lZDI1NTE5AAAAIF97N8ItHz5XLJ8vvcVtLJZs9qPZ7nYPQJ9jCLr2Tf1d";
    private const string DifferentEncodedKey =
        "AAAAC3NzaC1lZDI1NTE5AAAAIBlUS0rMZk7QH4cO+MVDWpNT4wzjlM2uM3A0YQ2grfRd";

    [TestMethod]
    public async Task Ct012MatchingKeyScanAllowsPinnedHost()
    {
        using var fixture = new VerifierFixture(EncodedKey);
        fixture.Runner.ObservedKey = EncodedKey;

        var result = await fixture.Verifier.VerifyAsync(fixture.Endpoint, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        var invocation = fixture.Runner.Invocations.Single();
        Assert.IsFalse(invocation.CompleteOnFirstOutputLine);
        Assert.IsNotNull(invocation.CompleteWhenFileContainsData);
        CollectionAssert.Contains(invocation.Arguments.ToArray(), "StrictHostKeyChecking=accept-new");
        CollectionAssert.Contains(invocation.Arguments.ToArray(), "PubkeyAuthentication=no");
        CollectionAssert.Contains(invocation.Arguments.ToArray(), "PreferredAuthentications=none");
        CollectionAssert.DoesNotContain(invocation.Arguments.ToArray(), "-i");
        StringAssert.EndsWith(invocation.ExecutablePath, "ssh.exe");
    }

    [TestMethod]
    public async Task Ct012DivergentScannedKeyReturnsNonRetryableIdentityError()
    {
        using var fixture = new VerifierFixture(EncodedKey);
        fixture.Runner.ObservedKey = DifferentEncodedKey;

        var result = await fixture.Verifier.VerifyAsync(fixture.Endpoint, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ErrorCode.ERR010, result.Error!.Code);
        Assert.IsFalse(result.Error.IsRetryable);
    }

    [TestMethod]
    public async Task Ct016MissingScannedKeyReturnsRetryableBridgeError()
    {
        using var fixture = new VerifierFixture(EncodedKey);
        fixture.Runner.ObservedKey = null;

        var result = await fixture.Verifier.VerifyAsync(fixture.Endpoint, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ErrorCode.ERR009, result.Error!.Code);
        Assert.IsTrue(result.Error.IsRetryable);
    }

    private sealed class VerifierFixture : IDisposable
    {
        public VerifierFixture(string pinnedKey)
        {
            Root = Path.Combine(Path.GetTempPath(), "wakepilot-host-key-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
            var sshPath = Path.GetFullPath(Path.Combine(Root, "ssh.exe"));
            var knownHostsPath = Path.GetFullPath(Path.Combine(Root, "known_hosts"));
            File.WriteAllText(
                knownHostsPath,
                $"[100.64.0.10]:8023 ssh-ed25519 {pinnedKey}{Environment.NewLine}",
                System.Text.Encoding.ASCII);
            Endpoint = new SshBridgeEndpoint(
                "100.64.0.10",
                8023,
                "u0_a123",
                TargetId.From(Guid.Parse("22222222-2222-4222-8222-222222222222")));
            var options = new SshBridgeOptions(
                sshPath,
                knownHostsPath,
                new Dictionary<BridgeId, SshBridgeEndpoint>());
            Verifier = new SshPinnedHostKeyVerifier(Runner, options);
        }

        public string Root { get; }

        public ProbeProcessRunner Runner { get; } = new();

        public SshBridgeEndpoint Endpoint { get; }

        public SshPinnedHostKeyVerifier Verifier { get; }

        public void Dispose()
        {
            Directory.Delete(Root, recursive: true);
        }
    }

    private sealed class ProbeProcessRunner : RemoteWake.Application.Ports.IProcessRunner
    {
        public string? ObservedKey { get; set; }

        public List<RemoteWake.Application.Models.ProcessInvocation> Invocations { get; } = [];

        public ValueTask<RemoteWake.Application.Models.ProcessExecutionResult> RunAsync(
            RemoteWake.Application.Models.ProcessInvocation invocation,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Invocations.Add(invocation);
            if (ObservedKey is not null)
            {
                var option = invocation.Arguments.Single(argument =>
                    argument.StartsWith("UserKnownHostsFile=", StringComparison.Ordinal));
                var path = option["UserKnownHostsFile=".Length..];
                File.WriteAllText(
                    path,
                    $"[100.64.0.10]:8023 ssh-ed25519 {ObservedKey}{Environment.NewLine}",
                    System.Text.Encoding.ASCII);
            }

            return ValueTask.FromResult(new RemoteWake.Application.Models.ProcessExecutionResult(
                255,
                string.Empty,
                "Permission denied"));
        }
    }
}
