using RemoteWake.Application.Models;

namespace RemoteWake.Launcher.Wpf.Services;

public interface IWakeLauncherService
{
    ValueTask<WakeExecutionResult> ExecuteAsync(
        WakeProfile profile,
        IProgress<WakeProgressUpdate> progress,
        CancellationToken cancellationToken);
}
