using RemoteWake.Launcher.Wpf.Services;

namespace RemoteWake.Launcher.Tests;

[TestClass]
public sealed class WindowsLauncherNotificationServiceTests
{
    private static readonly DateTimeOffset InitialTime =
        new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task SuccessUsesOnlyAllowlistedLocalizedContent()
    {
        var presenter = new RecordingPresenter();
        using var service = CreateService(presenter);

        var shown = await service.TryShowAsync(
            LauncherNotificationKind.Success,
            CancellationToken.None);

        Assert.IsTrue(shown);
        Assert.HasCount(1, presenter.Messages);
        Assert.AreEqual("NotificationSuccessTitle", presenter.Messages[0].Title);
        Assert.AreEqual("NotificationSuccessMessage", presenter.Messages[0].Message);
    }

    [TestMethod]
    public async Task DuplicateKindIsCoalescedInsideTenSecondWindow()
    {
        var presenter = new RecordingPresenter();
        var timeProvider = new AdjustableTimeProvider(InitialTime);
        using var service = CreateService(presenter, timeProvider);

        var first = await service.TryShowAsync(
            LauncherNotificationKind.ActionRequired,
            CancellationToken.None);
        timeProvider.Advance(TimeSpan.FromSeconds(9));
        var duplicate = await service.TryShowAsync(
            LauncherNotificationKind.ActionRequired,
            CancellationToken.None);

        Assert.IsTrue(first);
        Assert.IsTrue(duplicate);
        Assert.HasCount(1, presenter.Messages);
    }

    [TestMethod]
    public async Task DifferentKindIsShownWithoutWaitingForCoalescingWindow()
    {
        var presenter = new RecordingPresenter();
        using var service = CreateService(presenter);

        _ = await service.TryShowAsync(LauncherNotificationKind.Success, CancellationToken.None);
        _ = await service.TryShowAsync(LauncherNotificationKind.ActionRequired, CancellationToken.None);

        Assert.HasCount(2, presenter.Messages);
        Assert.AreEqual("NotificationActionTitle", presenter.Messages[1].Title);
        Assert.AreEqual("NotificationActionMessage", presenter.Messages[1].Message);
    }

    [TestMethod]
    public async Task PresenterFailureIsNonBlockingAndCanBeRetried()
    {
        var presenter = new RecordingPresenter { ThrowOnNextCall = true };
        using var service = CreateService(presenter);

        var failed = await service.TryShowAsync(
            LauncherNotificationKind.Success,
            CancellationToken.None);
        var retried = await service.TryShowAsync(
            LauncherNotificationKind.Success,
            CancellationToken.None);

        Assert.IsFalse(failed);
        Assert.IsTrue(retried);
        Assert.HasCount(1, presenter.Messages);
    }

    [TestMethod]
    public async Task CancellationDoesNotDisplayNotification()
    {
        var presenter = new RecordingPresenter();
        using var service = CreateService(presenter);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            async () => await service.TryShowAsync(
                LauncherNotificationKind.Success,
                cancellation.Token));
        Assert.IsEmpty(presenter.Messages);
    }

    [TestMethod]
    public void DisposeReleasesNativePresenter()
    {
        var presenter = new RecordingPresenter();
        var service = CreateService(presenter);

        service.Dispose();

        Assert.IsTrue(presenter.IsDisposed);
    }

    private static WindowsLauncherNotificationService CreateService(
        RecordingPresenter presenter,
        TimeProvider? timeProvider = null) =>
        new(presenter, new KeyTextProvider(), timeProvider ?? new AdjustableTimeProvider(InitialTime));

    private sealed class KeyTextProvider : ILauncherTextProvider
    {
        public string GetText(string resourceKey) => resourceKey;
    }

    private sealed class RecordingPresenter : INativeNotificationPresenter, IDisposable
    {
        public List<(string Title, string Message)> Messages { get; } = [];

        public bool ThrowOnNextCall { get; set; }

        public bool IsDisposed { get; private set; }

        public void Show(string title, string message)
        {
            if (ThrowOnNextCall)
            {
                ThrowOnNextCall = false;
                throw new InvalidOperationException("Simulated notification failure.");
            }

            Messages.Add((title, message));
        }

        public void Dispose() => IsDisposed = true;
    }

    private sealed class AdjustableTimeProvider(DateTimeOffset current) : TimeProvider
    {
        private DateTimeOffset current = current;

        public override DateTimeOffset GetUtcNow() => current;

        public void Advance(TimeSpan duration) => current += duration;
    }
}
