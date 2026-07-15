using RemoteWake.Application.Models;

namespace RemoteWake.Launcher.Wpf.Services;

internal sealed class UnavailableWakeLauncherService : IWakeLauncherService
{
    public ValueTask<WakeExecutionResult> ExecuteAsync(
        WakeProfile profile,
        IProgress<WakeProgressUpdate> progress,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("The launcher has no operational profile.");
}
