using System.Drawing;
using System.Windows.Threading;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using ToolTipIcon = System.Windows.Forms.ToolTipIcon;

namespace RemoteWake.Launcher.Wpf.Services;

public interface INativeNotificationPresenter
{
    void Show(string title, string message);
}

public sealed class WindowsLauncherNotificationService : ILauncherNotificationService, IDisposable
{
    internal static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(10);

    private readonly INativeNotificationPresenter presenter;
    private readonly ILauncherTextProvider texts;
    private readonly TimeProvider timeProvider;
    private LauncherNotificationKind? lastKind;
    private DateTimeOffset lastShownAt;
    private bool isDisposed;

    public WindowsLauncherNotificationService(
        INativeNotificationPresenter presenter,
        ILauncherTextProvider texts,
        TimeProvider timeProvider)
    {
        this.presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        this.texts = texts ?? throw new ArgumentNullException(nameof(texts));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public ValueTask<bool> TryShowAsync(
        LauncherNotificationKind kind,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(isDisposed, this);

        var now = timeProvider.GetUtcNow();
        if (lastKind == kind && now - lastShownAt < DuplicateWindow)
        {
            return ValueTask.FromResult(true);
        }

        var (titleKey, messageKey) = kind switch
        {
            LauncherNotificationKind.Success =>
                ("NotificationSuccessTitle", "NotificationSuccessMessage"),
            LauncherNotificationKind.ActionRequired =>
                ("NotificationActionTitle", "NotificationActionMessage"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        try
        {
            presenter.Show(texts.GetText(titleKey), texts.GetText(messageKey));
            lastKind = kind;
            lastShownAt = now;
            return ValueTask.FromResult(true);
        }
        catch (Exception)
        {
            return ValueTask.FromResult(false);
        }
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        if (presenter is IDisposable disposablePresenter)
        {
            disposablePresenter.Dispose();
        }

        isDisposed = true;
        GC.SuppressFinalize(this);
    }
}

public sealed class SystemTrayNotificationPresenter : INativeNotificationPresenter, IDisposable
{
    private static readonly TimeSpan VisibilityDuration = TimeSpan.FromSeconds(7);
    private readonly NotifyIcon notifyIcon;
    private readonly DispatcherTimer visibilityTimer;
    private bool isDisposed;

    public SystemTrayNotificationPresenter()
    {
        notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Text = "WakePilot",
            Visible = false,
        };
        visibilityTimer = new DispatcherTimer
        {
            Interval = VisibilityDuration,
        };
        visibilityTimer.Tick += HideIcon;
    }

    public void Show(string title, string message)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        visibilityTimer.Stop();
        notifyIcon.Visible = true;
        notifyIcon.ShowBalloonTip(
            (int)TimeSpan.FromSeconds(5).TotalMilliseconds,
            title,
            message,
            ToolTipIcon.Info);
        visibilityTimer.Start();
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        visibilityTimer.Stop();
        visibilityTimer.Tick -= HideIcon;
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        isDisposed = true;
        GC.SuppressFinalize(this);
    }

    private void HideIcon(object? sender, EventArgs e)
    {
        visibilityTimer.Stop();
        notifyIcon.Visible = false;
    }
}
