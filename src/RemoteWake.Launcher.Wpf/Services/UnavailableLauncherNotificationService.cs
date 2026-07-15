namespace RemoteWake.Launcher.Wpf.Services;

internal sealed class UnavailableLauncherNotificationService : ILauncherNotificationService
{
    public ValueTask<bool> TryShowAsync(
        LauncherNotificationKind kind,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(false);
    }
}
