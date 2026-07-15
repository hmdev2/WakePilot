using RemoteWake.Application.Models;

namespace RemoteWake.Launcher.Wpf.Services;

public interface ILauncherStatusService
{
    ValueTask<WakeStatusSnapshot> RefreshAsync(
        WakeProfile profile,
        CancellationToken cancellationToken);
}
