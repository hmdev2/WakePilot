using RemoteWake.Application.Models;
using RemoteWake.Application.Observability;
using RemoteWake.Application.Ports;
using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Results;
using RemoteWake.Domain.Security;
using RemoteWake.Domain.Time;

namespace RemoteWake.Application.Tests.Fakes;

internal sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; private set; } =
        new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);

    public List<TimeSpan> Delays { get; } = [];

    public Action? BeforeDelayCompletion { get; set; }

    public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        Delays.Add(delay);
        UtcNow += delay;
        BeforeDelayCompletion?.Invoke();
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }
}

internal sealed class FakeNonceGenerator : INonceGenerator
{
    public Nonce Create() => Nonce.Parse(new string('A', 43));
}

internal sealed class FakeVpnAdapter : IVpnAdapter
{
    public Result<VpnStatus> Status { get; set; } = Result.Success(new VpnStatus(true));

    public int Calls { get; private set; }

    public ValueTask<Result<VpnStatus>> GetStatusAsync(
        BridgeId bridgeId,
        CancellationToken cancellationToken)
    {
        Calls++;
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Status);
    }
}

internal sealed class FakeBridgeClient : IBridgeClient
{
    public Result<BridgeHealth> Health { get; set; } =
        Result.Success(new BridgeHealth(true, true));

    public Func<WakeCommand, Result<WakeReceipt>> ReceiptFactory { get; set; } =
        command => Result.Success(new WakeReceipt(command.RequestId, true, 3));

    public int HealthCalls { get; private set; }

    public List<WakeCommand> SentCommands { get; } = [];

    public ValueTask<Result<BridgeHealth>> GetHealthAsync(
        BridgeId bridgeId,
        CancellationToken cancellationToken)
    {
        HealthCalls++;
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Health);
    }

    public ValueTask<Result<WakeReceipt>> SendWakeAsync(
        BridgeId bridgeId,
        WakeCommand command,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SentCommands.Add(command);
        return ValueTask.FromResult(ReceiptFactory(command));
    }
}

internal sealed class FakeWakeStateProbe : IWakeStateProbe
{
    private readonly Queue<Result<WindowsReadiness>> windows = [];
    private readonly Queue<Result<RemoteServiceReadiness>> services = [];

    public List<string> Calls { get; } = [];

    public void EnqueueWindows(params Result<WindowsReadiness>[] results)
    {
        foreach (var result in results)
        {
            windows.Enqueue(result);
        }
    }

    public void EnqueueServices(params Result<RemoteServiceReadiness>[] results)
    {
        foreach (var result in results)
        {
            services.Enqueue(result);
        }
    }

    public ValueTask<Result<WindowsReadiness>> ProbeWindowsAsync(
        ComputerId computerId,
        CancellationToken cancellationToken)
    {
        Calls.Add("windows");
        cancellationToken.ThrowIfCancellationRequested();
        var result = windows.Count > 0
            ? windows.Dequeue()
            : Result.Success(new WindowsReadiness(false));
        return ValueTask.FromResult(result);
    }

    public ValueTask<Result<RemoteServiceReadiness>> ProbeRemoteServiceAsync(
        ComputerId computerId,
        string requestedServiceId,
        CancellationToken cancellationToken)
    {
        Calls.Add("service");
        cancellationToken.ThrowIfCancellationRequested();
        var result = services.Count > 0
            ? services.Dequeue()
            : Result.Success(new RemoteServiceReadiness(false));
        return ValueTask.FromResult(result);
    }
}

internal sealed class FakeRemoteAppAdapter : IRemoteAppAdapter
{
    public Result<LaunchResult> LaunchResult { get; set; } =
        Result.Success(new LaunchResult(true));

    public int Calls { get; private set; }

    public ValueTask<Result<LaunchResult>> LaunchAsync(
        WakeProfile profile,
        CancellationToken cancellationToken)
    {
        Calls++;
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(LaunchResult);
    }
}

internal sealed class FakeAuditSink : IAuditSink
{
    public List<SanitizedLogEvent> Events { get; } = [];

    public Result WriteResult { get; set; } = Result.Success();

    public ValueTask<Result> WriteAsync(
        SanitizedLogEvent logEvent,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Events.Add(logEvent);
        return ValueTask.FromResult(WriteResult);
    }
}

internal sealed class FakeWindowsDiagnostics : IWindowsDiagnostics
{
    public ValueTask<Result<WindowsDiagnosticSnapshot>> CollectAsync(
        ComputerId computerId,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(Result.Success(
            new WindowsDiagnosticSnapshot(DateTimeOffset.UnixEpoch, "detected")));
}

internal sealed class FakeSecretVault : ISecretVault
{
    private ReadOnlyMemory<byte> stored;

    public ValueTask<Result<SecretReference>> StoreAsync(
        string purpose,
        ReadOnlyMemory<byte> secret,
        CancellationToken cancellationToken)
    {
        stored = secret.ToArray();
        return ValueTask.FromResult(Result.Success(new SecretReference(purpose)));
    }

    public ValueTask<Result<ReadOnlyMemory<byte>>> RetrieveAsync(
        SecretReference reference,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(Result.Success(stored));

    public ValueTask<Result> RemoveAsync(
        SecretReference reference,
        CancellationToken cancellationToken)
    {
        stored = ReadOnlyMemory<byte>.Empty;
        return ValueTask.FromResult(Result.Success());
    }
}

internal sealed class FakeRepository : IRepository
{
    public WakeAttemptSnapshot? SavedAttempt { get; private set; }

    public ValueTask<Result<ComputerProfileSnapshot?>> GetComputerProfileAsync(
        ComputerId computerId,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(Result.Success<ComputerProfileSnapshot?>(
            new ComputerProfileSnapshot(computerId, "Test PC")));

    public ValueTask<Result> SaveWakeAttemptAsync(
        WakeAttemptSnapshot attempt,
        CancellationToken cancellationToken)
    {
        SavedAttempt = attempt;
        return ValueTask.FromResult(Result.Success());
    }
}

internal sealed class FakePrivilegedOperations : IPrivilegedOperations
{
    public ValueTask<Result<PrivilegedOperationResult>> ExecuteAsync(
        PrivilegedOperationRequest request,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(Result.Success(new PrivilegedOperationResult(true, true)));
}
