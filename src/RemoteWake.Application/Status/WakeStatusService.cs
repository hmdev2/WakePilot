using RemoteWake.Application.Models;
using RemoteWake.Application.Ports;
using RemoteWake.Domain.Results;
using RemoteWake.Domain.Time;

namespace RemoteWake.Application.Status;

public sealed class WakeStatusService
{
    private readonly IVpnAdapter vpnAdapter;
    private readonly IBridgeClient bridgeClient;
    private readonly IWakeStateProbe wakeStateProbe;
    private readonly IClock clock;

    public WakeStatusService(
        IVpnAdapter vpnAdapter,
        IBridgeClient bridgeClient,
        IWakeStateProbe wakeStateProbe,
        IClock clock)
    {
        this.vpnAdapter = vpnAdapter ?? throw new ArgumentNullException(nameof(vpnAdapter));
        this.bridgeClient = bridgeClient ?? throw new ArgumentNullException(nameof(bridgeClient));
        this.wakeStateProbe = wakeStateProbe ?? throw new ArgumentNullException(nameof(wakeStateProbe));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async ValueTask<WakeStatusSnapshot> RefreshAsync(
        WakeProfile profile,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var computerTask = GetComputerStatusAsync(profile, cancellationToken).AsTask();
        var bridgeTask = GetBridgeStatusAsync(profile, cancellationToken).AsTask();
        await Task.WhenAll(computerTask, bridgeTask).ConfigureAwait(false);

        var computer = await computerTask.ConfigureAwait(false);
        var bridge = await bridgeTask.ConfigureAwait(false);
        return new WakeStatusSnapshot(
            computer.Computer,
            bridge.Bridge,
            computer.RemoteApplication,
            clock.UtcNow,
            computer.ComputerError,
            bridge.Error,
            computer.RemoteApplicationError);
    }

    private async ValueTask<ComputerStatus> GetComputerStatusAsync(
        WakeProfile profile,
        CancellationToken cancellationToken)
    {
        var windows = await wakeStateProbe
            .ProbeWindowsAsync(profile.ComputerId, cancellationToken)
            .ConfigureAwait(false);
        if (windows.IsFailure)
        {
            return new ComputerStatus(
                ComputerOperationalState.Unknown,
                RemoteApplicationOperationalState.Unknown,
                windows.Error!.Code,
                null);
        }

        if (!windows.Value.IsReady)
        {
            return new ComputerStatus(
                ComputerOperationalState.NotReady,
                RemoteApplicationOperationalState.Unknown,
                null,
                null);
        }

        var service = await wakeStateProbe
            .ProbeRemoteServiceAsync(profile.ComputerId, profile.RequestedServiceId, cancellationToken)
            .ConfigureAwait(false);
        if (service.IsFailure)
        {
            return new ComputerStatus(
                ComputerOperationalState.Ready,
                RemoteApplicationOperationalState.Unknown,
                null,
                service.Error!.Code);
        }

        return new ComputerStatus(
            ComputerOperationalState.Ready,
            service.Value.IsReady
                ? RemoteApplicationOperationalState.Ready
                : RemoteApplicationOperationalState.NotReady,
            null,
            null);
    }

    private async ValueTask<BridgeStatus> GetBridgeStatusAsync(
        WakeProfile profile,
        CancellationToken cancellationToken)
    {
        var vpn = await vpnAdapter.GetStatusAsync(profile.BridgeId, cancellationToken).ConfigureAwait(false);
        if (vpn.IsFailure)
        {
            return new BridgeStatus(GetBridgeState(vpn.Error!.Code), vpn.Error.Code);
        }

        if (!vpn.Value.IsConnected)
        {
            return new BridgeStatus(BridgeOperationalState.VpnDisconnected, ErrorCode.ERR008);
        }

        var health = await bridgeClient.GetHealthAsync(profile.BridgeId, cancellationToken).ConfigureAwait(false);
        if (health.IsFailure)
        {
            return new BridgeStatus(GetBridgeState(health.Error!.Code), health.Error.Code);
        }

        return health.Value.CanWake
            ? new BridgeStatus(BridgeOperationalState.Ready, null)
            : new BridgeStatus(BridgeOperationalState.Unavailable, ErrorCode.ERR009);
    }

    private static BridgeOperationalState GetBridgeState(ErrorCode code) => code switch
    {
        ErrorCode.ERR008 => BridgeOperationalState.VpnDisconnected,
        ErrorCode.ERR009 => BridgeOperationalState.Unavailable,
        ErrorCode.ERR010 => BridgeOperationalState.IdentityMismatch,
        _ => BridgeOperationalState.Unknown,
    };

    private sealed record ComputerStatus(
        ComputerOperationalState Computer,
        RemoteApplicationOperationalState RemoteApplication,
        ErrorCode? ComputerError,
        ErrorCode? RemoteApplicationError);

    private sealed record BridgeStatus(BridgeOperationalState Bridge, ErrorCode? Error);
}
