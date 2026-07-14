using RemoteWake.Application.Models;
using RemoteWake.Application.Observability;
using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Results;

namespace RemoteWake.Application.Ports;

public interface IVpnAdapter
{
    ValueTask<Result<VpnStatus>> GetStatusAsync(
        BridgeId bridgeId,
        CancellationToken cancellationToken);
}

public interface IBridgeClient
{
    ValueTask<Result<BridgeHealth>> GetHealthAsync(
        BridgeId bridgeId,
        CancellationToken cancellationToken);

    ValueTask<Result<WakeReceipt>> SendWakeAsync(
        BridgeId bridgeId,
        WakeCommand command,
        CancellationToken cancellationToken);
}

public interface IWindowsDiagnostics
{
    ValueTask<Result<WindowsDiagnosticSnapshot>> CollectAsync(
        ComputerId computerId,
        CancellationToken cancellationToken);
}

public interface IWakeStateProbe
{
    ValueTask<Result<WindowsReadiness>> ProbeWindowsAsync(
        ComputerId computerId,
        CancellationToken cancellationToken);

    ValueTask<Result<RemoteServiceReadiness>> ProbeRemoteServiceAsync(
        ComputerId computerId,
        string requestedServiceId,
        CancellationToken cancellationToken);
}

public interface IRemoteAppAdapter
{
    ValueTask<Result<LaunchResult>> LaunchAsync(
        WakeProfile profile,
        CancellationToken cancellationToken);
}

public interface ISecretVault
{
    ValueTask<Result<SecretReference>> StoreAsync(
        string purpose,
        ReadOnlyMemory<byte> secret,
        CancellationToken cancellationToken);

    ValueTask<Result<ReadOnlyMemory<byte>>> RetrieveAsync(
        SecretReference reference,
        CancellationToken cancellationToken);

    ValueTask<Result> RemoveAsync(
        SecretReference reference,
        CancellationToken cancellationToken);
}

public interface IRepository
{
    ValueTask<Result<ComputerProfileSnapshot?>> GetComputerProfileAsync(
        ComputerId computerId,
        CancellationToken cancellationToken);

    ValueTask<Result> SaveWakeAttemptAsync(
        WakeAttemptSnapshot attempt,
        CancellationToken cancellationToken);
}

public interface IPrivilegedOperations
{
    ValueTask<Result<PrivilegedOperationResult>> ExecuteAsync(
        PrivilegedOperationRequest request,
        CancellationToken cancellationToken);
}

public interface IAuditSink
{
    ValueTask<Result> WriteAsync(
        SanitizedLogEvent logEvent,
        CancellationToken cancellationToken);
}

public interface IBackoffPolicy
{
    TimeSpan GetDelay(int attempt);
}

