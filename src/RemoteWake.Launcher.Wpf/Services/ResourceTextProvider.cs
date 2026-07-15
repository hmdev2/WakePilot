using System.Windows;

namespace RemoteWake.Launcher.Wpf.Services;

public sealed class ResourceTextProvider : ILauncherTextProvider
{
    private readonly System.Windows.Application application;

    public ResourceTextProvider(System.Windows.Application application)
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
    }

    public string GetText(string resourceKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceKey);
        return application.TryFindResource(resourceKey) as string
            ?? throw new InvalidOperationException($"Missing UI resource: {resourceKey}");
    }
}
