using RemoteWake.Application.Models;
using RemoteWake.Application.Ports;
using RemoteWake.Domain.Security;
using RemoteWake.Domain.Results;
using RemoteWake.Domain.Time;

namespace RemoteWake.M1.ContractTests;

internal sealed class RecordingProcessRunner : IProcessRunner
{
    private readonly Queue<ProcessExecutionResult> results = new();

    public List<ProcessInvocation> Invocations { get; } = [];

    public void Enqueue(ProcessExecutionResult result) => results.Enqueue(result);

    public ValueTask<ProcessExecutionResult> RunAsync(
        ProcessInvocation invocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Invocations.Add(invocation);
        return ValueTask.FromResult(results.Dequeue());
    }
}

internal sealed class FixedClock(DateTimeOffset value) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = value;

    public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        UtcNow += delay;
        return ValueTask.CompletedTask;
    }
}

internal sealed class FixedNonceGenerator(string value) : INonceGenerator
{
    public Nonce Create() => Nonce.Parse(value);
}

internal sealed class RecordingPrivateKeyLeaseProvider(string path) : IPrivateKeyLeaseProvider
{
    public int AcquireCount { get; private set; }

    public bool IsDisposed { get; private set; }

    public ValueTask<Result<IPrivateKeyLease>> AcquireAsync(
        RemoteWake.Domain.Identifiers.BridgeId bridgeId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bridgeId);
        cancellationToken.ThrowIfCancellationRequested();
        AcquireCount++;
        return ValueTask.FromResult(Result.Success<IPrivateKeyLease>(new Lease(this, path)));
    }

    private sealed class Lease(RecordingPrivateKeyLeaseProvider owner, string path) : IPrivateKeyLease
    {
        public string FilePath { get; } = path;

        public ValueTask DisposeAsync()
        {
            owner.IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
