using RemoteWake.Application.Models;
using RemoteWake.Application.Orchestration;

namespace RemoteWake.Launcher.Wpf.Services;

public sealed class WakeLauncherService : IWakeLauncherService
{
    private readonly WakeOrchestrator orchestrator;

    public WakeLauncherService(WakeOrchestrator orchestrator)
    {
        this.orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    public ValueTask<WakeExecutionResult> ExecuteAsync(
        WakeProfile profile,
        IProgress<WakeProgressUpdate> progress,
        CancellationToken cancellationToken) =>
        orchestrator.ExecuteAsync(profile, progress, cancellationToken);
}
