namespace RemoteWake.Launcher.Wpf.Services;

public enum LauncherNotificationKind
{
    Success,
    ActionRequired,
}

public interface ILauncherNotificationService
{
    ValueTask<bool> TryShowAsync(
        LauncherNotificationKind kind,
        CancellationToken cancellationToken);
}
