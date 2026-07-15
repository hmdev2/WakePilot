using System.Windows;
using RemoteWake.Application.Models;
using RemoteWake.Application.Orchestration;
using RemoteWake.Application.Ports;
using RemoteWake.Application.Timing;
using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Results;
using RemoteWake.Domain.Security;
using RemoteWake.Domain.Time;
using RemoteWake.Launcher.Wpf.Services;
using RemoteWake.Launcher.Wpf.ViewModels;

namespace RemoteWake.Launcher.Wpf;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var isDemo = e.Args.Contains("--demo", StringComparer.OrdinalIgnoreCase);
#if DEBUG
        isDemo = true;
#endif
        var texts = new ResourceTextProvider(this);
        var notifications = new WindowsLauncherNotificationService(
            new SystemTrayNotificationPresenter(),
            texts,
            TimeProvider.System);
        var composition = isDemo
            ? CreateDemoComposition(notifications)
            : CreateUnconfiguredComposition(notifications);
        var viewModel = new LauncherViewModel(
            composition.Service,
            composition.StatusService,
            composition.NotificationService,
            texts,
            composition.Profile,
            texts.GetText("ComputerDefaultName"),
            isDemo);

        var window = new MainWindow(viewModel);
        MainWindow = window;
        window.Show();
    }

    private static LauncherComposition CreateDemoComposition(
        ILauncherNotificationService notificationService)
    {
        var orchestrator = new WakeOrchestrator(
            new DemoVpnAdapter(),
            new DemoBridgeClient(),
            new DemoWakeStateProbe(),
            new DemoRemoteAppAdapter(),
            new DemoAuditSink(),
            SystemClock.Instance,
            new CryptographicNonceGenerator(),
            new ReadinessBackoffPolicy());
        var profile = new WakeProfile(
            ComputerId.New(),
            BridgeId.New(),
            TargetId.New(),
            "rustdesk");
        return new LauncherComposition(
            new WakeLauncherService(orchestrator),
            new DemoLauncherStatusService(),
            notificationService,
            profile);
    }

    private static LauncherComposition CreateUnconfiguredComposition(
        ILauncherNotificationService notificationService) =>
        new(
            new UnavailableWakeLauncherService(),
            new UnavailableLauncherStatusService(),
            notificationService,
            null);

    private sealed record LauncherComposition(
        IWakeLauncherService Service,
        ILauncherStatusService StatusService,
        ILauncherNotificationService NotificationService,
        WakeProfile? Profile);

    private sealed class DemoLauncherStatusService : ILauncherStatusService
    {
        public ValueTask<WakeStatusSnapshot> RefreshAsync(
            WakeProfile profile,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new WakeStatusSnapshot(
                ComputerOperationalState.NotReady,
                BridgeOperationalState.Ready,
                RemoteApplicationOperationalState.Unknown,
                DateTimeOffset.UtcNow));
        }
    }

    private sealed class DemoVpnAdapter : IVpnAdapter
    {
        public ValueTask<Result<VpnStatus>> GetStatusAsync(
            BridgeId bridgeId,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(Result.Success(new VpnStatus(true, IsPeerOnline: true)));
    }

    private sealed class DemoBridgeClient : IBridgeClient
    {
        public ValueTask<Result<BridgeHealth>> GetHealthAsync(
            BridgeId bridgeId,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(Result.Success(new BridgeHealth(true, true)));

        public ValueTask<Result<WakeReceipt>> SendWakeAsync(
            BridgeId bridgeId,
            WakeCommand command,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(Result.Success(new WakeReceipt(command.RequestId, true, 3)));
    }

    private sealed class DemoWakeStateProbe : IWakeStateProbe
    {
        private int windowsProbeCount;

        public ValueTask<Result<WindowsReadiness>> ProbeWindowsAsync(
            ComputerId computerId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var isReady = Interlocked.Increment(ref windowsProbeCount) > 1;
            return ValueTask.FromResult(Result.Success(new WindowsReadiness(isReady)));
        }

        public ValueTask<Result<RemoteServiceReadiness>> ProbeRemoteServiceAsync(
            ComputerId computerId,
            string requestedServiceId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(Result.Success(new RemoteServiceReadiness(true)));
        }
    }

    private sealed class DemoRemoteAppAdapter : IRemoteAppAdapter
    {
        public ValueTask<Result<LaunchResult>> LaunchAsync(
            WakeProfile profile,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(Result.Success(new LaunchResult(true)));
    }

    private sealed class DemoAuditSink : IAuditSink
    {
        public ValueTask<Result> WriteAsync(
            RemoteWake.Application.Observability.SanitizedLogEvent logEvent,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(Result.Success());
    }
}
