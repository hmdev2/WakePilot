using RemoteWake.Application.Models;
using RemoteWake.Application.Observability;
using RemoteWake.Application.Orchestration;
using RemoteWake.Application.Ports;
using RemoteWake.Application.Tests.Fakes;
using RemoteWake.Application.Timing;
using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Results;
using RemoteWake.Domain.Wake;

namespace RemoteWake.Application.Tests;

[TestClass]
public sealed class QualityAndPortsTests
{
    private static readonly int[] ExpectedBackoffSeconds = [1, 2, 4, 8, 10, 10, 10];

    [TestMethod]
    public void SanitizerAllowsOnlyKnownPropertiesAndRedactsSensitiveValues()
    {
        var properties = new Dictionary<string, string>
        {
            ["phase"] = "password=not-a-real-credential",
            ["state"] = "WaitingWindows\r\nInjected",
            ["token"] = "must-be-dropped",
            ["outcome"] = new string('x', 140),
        };

        var sanitized = LogSanitizer.SanitizeProperties(properties);

        Assert.HasCount(3, sanitized);
        Assert.AreEqual("[REDACTED]", sanitized["phase"]);
        Assert.AreEqual("WaitingWindows  Injected", sanitized["state"]);
        Assert.AreEqual(128, sanitized["outcome"].Length);
        Assert.IsFalse(sanitized.ContainsKey("token"));
    }

    [TestMethod]
    public void TransitionAndFailureEventsAreTypedAndSanitized()
    {
        var correlationId = CorrelationId.New();
        var transition = new WakeTransition(WakeState.Checking, WakeState.SendingWake, WakeTrigger.WakeRequired);
        var error = DomainError.Create(ErrorCode.ERR009, "bridge_not_healthy", true);

        var transitionEvent = LogSanitizer.CreateTransitionEvent(
            DateTimeOffset.UnixEpoch,
            correlationId,
            transition);
        var failureEvent = LogSanitizer.CreateFailureEvent(
            DateTimeOffset.UnixEpoch,
            correlationId,
            error,
            WakeState.Failed);

        Assert.AreEqual("SendingWake", transitionEvent.Properties["nextState"]);
        Assert.AreEqual(LogEventLevel.Error, failureEvent.Level);
        Assert.AreEqual(ErrorCode.ERR009, failureEvent.Code);
        Assert.AreEqual("True", failureEvent.Properties["retryable"]);
    }

    [TestMethod]
    public void BackoffPolicyUsesRequiredSequenceAndCapsAtTenSeconds()
    {
        var policy = new ReadinessBackoffPolicy();

        var delays = Enumerable.Range(0, 7).Select(policy.GetDelay).ToArray();

        CollectionAssert.AreEqual(
            ExpectedBackoffSeconds.Select(seconds => TimeSpan.FromSeconds(seconds)).ToArray(),
            delays);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => policy.GetDelay(-1));
    }

    [TestMethod]
    public void OptionsExposeNormativeTimeoutsAndRejectInvalidValues()
    {
        var defaults = new WakeOrchestratorOptions();

        Assert.AreEqual(TimeSpan.FromSeconds(5), defaults.BridgeTimeout);
        Assert.AreEqual(TimeSpan.FromSeconds(10), defaults.ReceiptTimeout);
        Assert.AreEqual(TimeSpan.FromSeconds(240), defaults.WindowsTimeout);
        Assert.AreEqual(TimeSpan.FromSeconds(120), defaults.ServiceTimeout);
        defaults.Validate();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new WakeOrchestratorOptions { WindowsTimeout = TimeSpan.Zero }.Validate());
    }

    [TestMethod]
    public async Task EveryRequiredPortHasAUsableFakeWithoutExternalSideEffects()
    {
        var computerId = ComputerId.New();
        var diagnostics = new FakeWindowsDiagnostics();
        var vault = new FakeSecretVault();
        var repository = new FakeRepository();
        var privileged = new FakePrivilegedOperations();

        var diagnostic = await diagnostics.CollectAsync(computerId, CancellationToken.None);
        var secretReference = await vault.StoreAsync("test-purpose", new byte[] { 1, 2, 3 }, CancellationToken.None);
        var recovered = await vault.RetrieveAsync(secretReference.Value, CancellationToken.None);
        var removed = await vault.RemoveAsync(secretReference.Value, CancellationToken.None);
        var profile = await repository.GetComputerProfileAsync(computerId, CancellationToken.None);
        var request = RequestId.New();
        var saved = await repository.SaveWakeAttemptAsync(
            new WakeAttemptSnapshot(request, computerId, WakeState.Checking, DateTimeOffset.UnixEpoch),
            CancellationToken.None);
        var applied = await privileged.ExecuteAsync(
            new PrivilegedOperationRequest(PrivilegedOperation.EnableWakeOnLan, computerId),
            CancellationToken.None);

        Assert.IsTrue(diagnostic.IsSuccess);
        Assert.AreEqual("detected", diagnostic.Value.EvidenceStatus);
        Assert.IsTrue(recovered.Value.Span.SequenceEqual(new byte[] { 1, 2, 3 }));
        Assert.IsTrue(removed.IsSuccess);
        Assert.AreEqual("Test PC", profile.Value!.DisplayName);
        Assert.IsTrue(saved.IsSuccess);
        Assert.AreEqual(request, repository.SavedAttempt!.RequestId);
        Assert.IsTrue(applied.Value.Applied && applied.Value.Verified);
    }

    [TestMethod]
    public void ArchitectureKeepsDomainAndApplicationFreeOfConcreteInfrastructure()
    {
        var domainReferences = typeof(WakeState).Assembly.GetReferencedAssemblies();
        var applicationReferences = typeof(WakeOrchestrator).Assembly.GetReferencedAssemblies();
        var forbiddenPrefixes = new[]
        {
            "Microsoft.Data.Sqlite",
            "PresentationFramework",
            "RemoteWake.Infrastructure",
            "Renci.SshNet",
        };

        Assert.IsTrue(domainReferences.All(reference => reference.Name!.StartsWith("System", StringComparison.Ordinal)));
        Assert.IsTrue(applicationReferences.Any(reference => reference.Name == "RemoteWake.Domain"));
        Assert.IsFalse(applicationReferences.Any(
            reference => forbiddenPrefixes.Any(
                prefix => reference.Name!.StartsWith(prefix, StringComparison.Ordinal))));

        var requiredPorts = new[]
        {
            typeof(IVpnAdapter),
            typeof(IBridgeClient),
            typeof(IWindowsDiagnostics),
            typeof(IWakeStateProbe),
            typeof(IRemoteAppAdapter),
            typeof(ISecretVault),
            typeof(IRepository),
            typeof(IPrivilegedOperations),
        };
        Assert.IsTrue(requiredPorts.All(type => type.IsInterface));
    }
}
