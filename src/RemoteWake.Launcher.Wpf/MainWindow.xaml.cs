using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using RemoteWake.Launcher.Wpf.ViewModels;

namespace RemoteWake.Launcher.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(LauncherViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (DataContext is LauncherViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            FocusCurrentScreen();
            await viewModel.RefreshAsync();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is LauncherViewModel viewModel)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnClosed(e);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LauncherViewModel.Screen))
        {
            _ = Dispatcher.InvokeAsync(FocusCurrentScreen, DispatcherPriority.ContextIdle);
        }
    }

    private void FocusCurrentScreen()
    {
        if (DataContext is not LauncherViewModel viewModel)
        {
            return;
        }

        _ = viewModel.Screen switch
        {
            LauncherScreen.Dashboard => DashboardHeading.Focus(),
            LauncherScreen.Progress => ProgressHeading.Focus(),
            LauncherScreen.Success => SuccessHeading.Focus(),
            LauncherScreen.Failure => FailureHeading.Focus(),
            _ => false,
        };
    }
}
