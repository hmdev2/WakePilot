using RemoteWake.Application.Models;
using RemoteWake.Application.Ports;
using RemoteWake.Domain.Security;
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
