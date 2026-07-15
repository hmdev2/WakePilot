using System.Windows;
using RemoteWake.Launcher.Wpf.ViewModels;

namespace RemoteWake.Launcher.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(LauncherViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnClosed(e);
    }
}
