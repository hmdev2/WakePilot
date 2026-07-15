using RemoteWake.Application.Models;
using RemoteWake.Application.Status;

namespace RemoteWake.Launcher.Wpf.Services;

public sealed class LauncherStatusService : ILauncherStatusService
{
    private readonly WakeStatusService statusService;

    public LauncherStatusService(WakeStatusService statusService)
    {
        this.statusService = statusService ?? throw new ArgumentNullException(nameof(statusService));
    }

    public ValueTask<WakeStatusSnapshot> RefreshAsync(
        WakeProfile profile,
        CancellationToken cancellationToken) =>
        statusService.RefreshAsync(profile, cancellationToken);
}
