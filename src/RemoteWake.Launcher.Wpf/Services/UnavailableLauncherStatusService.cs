using RemoteWake.Application.Models;

namespace RemoteWake.Launcher.Wpf.Services;

internal sealed class UnavailableLauncherStatusService : ILauncherStatusService
{
    public ValueTask<WakeStatusSnapshot> RefreshAsync(
        WakeProfile profile,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("The launcher has no operational profile.");
}
