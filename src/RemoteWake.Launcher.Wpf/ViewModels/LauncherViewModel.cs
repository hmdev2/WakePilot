using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RemoteWake.Application.Models;
using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Results;
using RemoteWake.Domain.Wake;
using RemoteWake.Launcher.Wpf.Services;

namespace RemoteWake.Launcher.Wpf.ViewModels;

public sealed class LauncherViewModel : INotifyPropertyChanged, IDisposable
{
    private static readonly Dictionary<WakeState, int> TimelineStages =
        new Dictionary<WakeState, int>
        {
            [WakeState.Checking] = 0,
            [WakeState.SendingWake] = 0,
            [WakeState.WaitingWindows] = 1,
            [WakeState.WaitingService] = 3,
            [WakeState.OpeningClient] = 3,
            [WakeState.Completed] = 4,
        };

    private readonly IWakeLauncherService launcherService;
    private readonly ILauncherStatusService statusService;
    private readonly ILauncherTextProvider texts;
    private readonly ErrorPresentationMapper errorMapper;
    private readonly WakeProfile? profile;
    private readonly AsyncRelayCommand startCommand;
    private readonly AsyncRelayCommand refreshCommand;
    private readonly RelayCommand cancelCommand;
    private readonly Stopwatch stopwatch = new();
    private CancellationTokenSource? activeOperation;
    private CancellationTokenSource? statusRefresh;
    private WakeStatusSnapshot? latestStatus;
    private LauncherScreen screen;
    private bool isBusy;
    private bool isRefreshing;
    private string computerStatus;
    private string bridgeStatus;
    private string applicationStatus;
    private string lastChecked;
    private string phaseTitle;
    private string phaseDescription;
    private string elapsedText;
    private string correlationIdText = string.Empty;
    private string errorTitle = string.Empty;
    private string errorConsequence = string.Empty;
    private string errorAction = string.Empty;
    private string errorCode = string.Empty;
    private string requestStepStatus;
    private string computerStepStatus;
    private string windowsStepStatus;
    private string clientStepStatus;

    public LauncherViewModel(
        IWakeLauncherService launcherService,
        ILauncherStatusService statusService,
        ILauncherTextProvider texts,
        WakeProfile? profile,
        string computerName,
        bool isDemo = false)
    {
        this.launcherService = launcherService ?? throw new ArgumentNullException(nameof(launcherService));
        this.statusService = statusService ?? throw new ArgumentNullException(nameof(statusService));
        this.texts = texts ?? throw new ArgumentNullException(nameof(texts));
        this.profile = profile;
        ArgumentException.ThrowIfNullOrWhiteSpace(computerName);
        errorMapper = new ErrorPresentationMapper(texts);
        ComputerName = computerName;
        IsDemo = isDemo;
        screen = LauncherScreen.Dashboard;
        phaseTitle = texts.GetText("ProgressTitle");
        phaseDescription = texts.GetText("ProgressChecking");
        elapsedText = texts.GetText("ElapsedInitial");
        requestStepStatus = texts.GetText("StepWaiting");
        computerStepStatus = texts.GetText("StepWaiting");
        windowsStepStatus = texts.GetText("StepWaiting");
        clientStepStatus = texts.GetText("StepWaiting");
        computerStatus = IsConfigured
            ? texts.GetText("StatusChecking")
            : texts.GetText("StatusConfigurationPending");
        bridgeStatus = IsConfigured
            ? texts.GetText("StatusChecking")
            : texts.GetText("StatusNotAvailable");
        applicationStatus = IsConfigured
            ? texts.GetText("StatusChecking")
            : texts.GetText("StatusNotConfigured");
        lastChecked = texts.GetText("StatusNotChecked");
        startCommand = new AsyncRelayCommand(StartAsync, () => CanStart);
        refreshCommand = new AsyncRelayCommand(RefreshAsync, () => CanRefresh);
        cancelCommand = new RelayCommand(Cancel, () => IsBusy);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ComputerName { get; }

    public bool IsDemo { get; }

    public bool IsConfigured => profile is not null;

    public bool IsDashboardVisible => Screen == LauncherScreen.Dashboard;

    public bool IsProgressVisible => Screen == LauncherScreen.Progress;

    public bool IsSuccessVisible => Screen == LauncherScreen.Success;

    public bool IsFailureVisible => Screen == LauncherScreen.Failure;

    public string ComputerStatus { get => computerStatus; private set => SetProperty(ref computerStatus, value); }

    public string BridgeStatus { get => bridgeStatus; private set => SetProperty(ref bridgeStatus, value); }

    public string ApplicationStatus { get => applicationStatus; private set => SetProperty(ref applicationStatus, value); }

    public string LastChecked { get => lastChecked; private set => SetProperty(ref lastChecked, value); }

    public string StartBlockedReason => GetStartBlockedReason();

    public bool CanStart => IsConfigured && !IsBusy && !IsRefreshing && latestStatus?.CanStart == true;

    public bool CanRefresh => IsConfigured && !IsBusy && !IsRefreshing;

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanRefresh));
                OnPropertyChanged(nameof(StartBlockedReason));
                startCommand.NotifyCanExecuteChanged();
                refreshCommand.NotifyCanExecuteChanged();
                cancelCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsRefreshing
    {
        get => isRefreshing;
        private set
        {
            if (SetProperty(ref isRefreshing, value))
            {
                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanRefresh));
                OnPropertyChanged(nameof(StartBlockedReason));
                startCommand.NotifyCanExecuteChanged();
                refreshCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public LauncherScreen Screen
    {
        get => screen;
        private set
        {
            if (SetProperty(ref screen, value))
            {
                OnPropertyChanged(nameof(IsDashboardVisible));
                OnPropertyChanged(nameof(IsProgressVisible));
                OnPropertyChanged(nameof(IsSuccessVisible));
                OnPropertyChanged(nameof(IsFailureVisible));
            }
        }
    }

    public string PhaseTitle { get => phaseTitle; private set => SetProperty(ref phaseTitle, value); }

    public string PhaseDescription { get => phaseDescription; private set => SetProperty(ref phaseDescription, value); }

    public string ElapsedText { get => elapsedText; private set => SetProperty(ref elapsedText, value); }

    public string CorrelationIdText { get => correlationIdText; private set => SetProperty(ref correlationIdText, value); }

    public string ErrorTitle { get => errorTitle; private set => SetProperty(ref errorTitle, value); }

    public string ErrorConsequence { get => errorConsequence; private set => SetProperty(ref errorConsequence, value); }

    public string ErrorAction { get => errorAction; private set => SetProperty(ref errorAction, value); }

    public string ErrorCodeText { get => errorCode; private set => SetProperty(ref errorCode, value); }

    public string RequestStepStatus { get => requestStepStatus; private set => SetProperty(ref requestStepStatus, value); }

    public string ComputerStepStatus { get => computerStepStatus; private set => SetProperty(ref computerStepStatus, value); }

    public string WindowsStepStatus { get => windowsStepStatus; private set => SetProperty(ref windowsStepStatus, value); }

    public string ClientStepStatus { get => clientStepStatus; private set => SetProperty(ref clientStepStatus, value); }

    public ICommand StartCommand => startCommand;

    public ICommand RefreshCommand => refreshCommand;

    public ICommand CancelCommand => cancelCommand;

    public ICommand BackCommand => new RelayCommand(ShowDashboard, () => !IsBusy);

    public async Task RefreshAsync()
    {
        if (!CanRefresh || profile is null)
        {
            return;
        }

        var refresh = new CancellationTokenSource();
        statusRefresh = refresh;
        IsRefreshing = true;
        ComputerStatus = texts.GetText("StatusChecking");
        BridgeStatus = texts.GetText("StatusChecking");
        ApplicationStatus = texts.GetText("StatusChecking");

        try
        {
            var status = await statusService.RefreshAsync(profile, refresh.Token);
            ApplyStatus(status);
        }
        catch (OperationCanceledException) when (refresh.IsCancellationRequested)
        {
            // Closing the window cancels a pending, read-only refresh.
        }
        catch (Exception)
        {
            latestStatus = null;
            ComputerStatus = texts.GetText("StatusUnknown");
            BridgeStatus = texts.GetText("StatusUnknown");
            ApplicationStatus = texts.GetText("StatusUnknown");
            LastChecked = texts.GetText("StatusCheckFailed");
        }
        finally
        {
            refresh.Dispose();
            if (ReferenceEquals(statusRefresh, refresh))
            {
                statusRefresh = null;
            }
            IsRefreshing = false;
            NotifyStatusDecisionChanged();
        }
    }

    public async Task StartAsync()
    {
        if (!CanStart || profile is null)
        {
            return;
        }

        ResetTimeline();
        Screen = LauncherScreen.Progress;
        IsBusy = true;
        var operation = new CancellationTokenSource();
        activeOperation = operation;
        stopwatch.Restart();
        var progress = new Progress<WakeProgressUpdate>(ApplyProgress);
        var elapsedTask = UpdateElapsedAsync(operation.Token);

        try
        {
            var result = await launcherService.ExecuteAsync(profile, progress, operation.Token);
            ApplyResult(result);
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
            PhaseDescription = texts.GetText("ProgressCancelled");
            MarkActiveStep(texts.GetText("StepCancelled"));
            Screen = LauncherScreen.Dashboard;
        }
        catch (Exception) when (!operation.IsCancellationRequested)
        {
            ApplyUnexpectedFailure();
        }
        finally
        {
            stopwatch.Stop();
            operation.Cancel();
            await IgnoreCancellationAsync(elapsedTask);
            operation.Dispose();
            if (ReferenceEquals(activeOperation, operation))
            {
                activeOperation = null;
            }
            IsBusy = false;
        }
    }

    public void Cancel() => activeOperation?.Cancel();

    public void Dispose()
    {
        activeOperation?.Cancel();
        statusRefresh?.Cancel();
        GC.SuppressFinalize(this);
    }

    private void ApplyStatus(WakeStatusSnapshot status)
    {
        latestStatus = status;
        ComputerStatus = texts.GetText(status.Computer switch
        {
            ComputerOperationalState.Ready => "StatusReady",
            ComputerOperationalState.NotReady => "StatusNotReady",
            _ => "StatusUnknown",
        });
        BridgeStatus = texts.GetText(status.Bridge switch
        {
            BridgeOperationalState.Ready => "StatusOnline",
            BridgeOperationalState.VpnDisconnected => "StatusVpnDisconnected",
            BridgeOperationalState.Unavailable => "StatusNotAvailable",
            BridgeOperationalState.IdentityMismatch => "StatusIdentityChanged",
            _ => "StatusUnknown",
        });
        ApplicationStatus = texts.GetText(status.RemoteApplication switch
        {
            RemoteApplicationOperationalState.Ready => "StatusReady",
            RemoteApplicationOperationalState.NotReady => "StatusStarting",
            _ => "StatusUnknown",
        });
        LastChecked = status.ObservedAt
            .ToLocalTime()
            .ToString("g", System.Globalization.CultureInfo.CurrentCulture);
        NotifyStatusDecisionChanged();
    }

    private string GetStartBlockedReason()
    {
        if (!IsConfigured)
        {
            return texts.GetText("StartBlockedReason");
        }

        if (IsRefreshing)
        {
            return texts.GetText("StatusCheckingReason");
        }

        if (latestStatus?.CanStart == true)
        {
            return string.Empty;
        }

        return latestStatus?.Bridge switch
        {
            BridgeOperationalState.IdentityMismatch => texts.GetText("ErrorIdentityAction"),
            BridgeOperationalState.VpnDisconnected => texts.GetText("ErrorVpnAction"),
            BridgeOperationalState.Unavailable => texts.GetText("ErrorBridgeAction"),
            _ => texts.GetText("StatusUnavailableReason"),
        };
    }

    private void NotifyStatusDecisionChanged()
    {
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(StartBlockedReason));
        startCommand.NotifyCanExecuteChanged();
    }

    private async Task UpdateElapsedAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                ElapsedText = string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    texts.GetText("ElapsedFormat"),
                    (int)stopwatch.Elapsed.TotalSeconds);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when the active operation finishes or is cancelled.
        }
    }

    private void ApplyProgress(WakeProgressUpdate update)
    {
        if (!IsBusy || Screen != LauncherScreen.Progress)
        {
            return;
        }

        CorrelationIdText = update.CorrelationId.ToString();
        PhaseDescription = texts.GetText(GetPhaseResourceKey(update.State));
        ApplyTimeline(update.State);
    }

    private void ApplyResult(WakeExecutionResult result)
    {
        CorrelationIdText = result.CorrelationId.ToString();
        foreach (var state in result.StateHistory)
        {
            ApplyTimeline(state);
        }

        ElapsedText = string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            texts.GetText("ElapsedFormat"),
            (int)result.Duration.TotalSeconds);

        if (result.IsSuccess)
        {
            ApplyTimeline(WakeState.Completed);
            Screen = LauncherScreen.Success;
            return;
        }

        if (result.FinalState == WakeState.Cancelled)
        {
            PhaseDescription = texts.GetText("ProgressCancelled");
            MarkActiveStep(texts.GetText("StepCancelled"));
            Screen = LauncherScreen.Dashboard;
            return;
        }

        ShowFailure(result.Error?.Code ?? ErrorCode.ERR020);
    }

    private void ApplyUnexpectedFailure()
    {
        CorrelationIdText = CorrelationId.New().ToString();
        ShowFailure(ErrorCode.ERR020);
    }

    private void ShowFailure(ErrorCode code)
    {
        var presentation = errorMapper.Map(code);
        ErrorTitle = presentation.Title;
        ErrorConsequence = presentation.Consequence;
        ErrorAction = presentation.Action;
        ErrorCodeText = code.ToString();
        MarkActiveStep(texts.GetText("StepFailed"));
        Screen = LauncherScreen.Failure;
    }

    private void ApplyTimeline(WakeState state)
    {
        if (state == WakeState.BridgeUnavailable)
        {
            RequestStepStatus = texts.GetText("StepFailed");
            return;
        }

        if (state == WakeState.AlreadyReady)
        {
            RequestStepStatus = texts.GetText("StepNotNeeded");
            ComputerStepStatus = texts.GetText("StepCompleted");
            WindowsStepStatus = texts.GetText("StepCompleted");
            ClientStepStatus = texts.GetText("StepInProgress");
            return;
        }

        if (!TimelineStages.TryGetValue(state, out var stage))
        {
            return;
        }

        RequestStepStatus = GetStageStatus(stage, 0);
        ComputerStepStatus = GetStageStatus(stage, 1);
        WindowsStepStatus = GetStageStatus(stage, 2);
        ClientStepStatus = GetStageStatus(stage, 3);
    }

    private string GetStageStatus(int currentStage, int step) =>
        currentStage > step
            ? texts.GetText("StepCompleted")
            : currentStage == step
                ? texts.GetText("StepInProgress")
                : texts.GetText("StepWaiting");

    private void MarkActiveStep(string status)
    {
        if (ClientStepStatus == texts.GetText("StepInProgress"))
        {
            ClientStepStatus = status;
        }
        else if (WindowsStepStatus == texts.GetText("StepInProgress"))
        {
            WindowsStepStatus = status;
        }
        else if (ComputerStepStatus == texts.GetText("StepInProgress"))
        {
            ComputerStepStatus = status;
        }
        else
        {
            RequestStepStatus = status;
        }
    }

    private void ResetTimeline()
    {
        RequestStepStatus = texts.GetText("StepWaiting");
        ComputerStepStatus = texts.GetText("StepWaiting");
        WindowsStepStatus = texts.GetText("StepWaiting");
        ClientStepStatus = texts.GetText("StepWaiting");
        PhaseTitle = texts.GetText("ProgressTitle");
        PhaseDescription = texts.GetText("ProgressChecking");
        CorrelationIdText = string.Empty;
    }

    private void ShowDashboard() => Screen = LauncherScreen.Dashboard;

    private static string GetPhaseResourceKey(WakeState state) => state switch
    {
        WakeState.Checking => "ProgressChecking",
        WakeState.AlreadyReady => "ProgressAlreadyReady",
        WakeState.BridgeUnavailable => "ProgressBridgeUnavailable",
        WakeState.SendingWake => "ProgressSendingWake",
        WakeState.WaitingWindows => "ProgressWaitingWindows",
        WakeState.WaitingService => "ProgressWaitingService",
        WakeState.OpeningClient => "ProgressOpeningClient",
        WakeState.Completed => "ProgressCompleted",
        WakeState.Failed => "ProgressFailed",
        WakeState.Cancelled => "ProgressCancelled",
        _ => "ProgressChecking",
    };

    private static async Task IgnoreCancellationAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // The timer shares the operation cancellation token.
        }
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
