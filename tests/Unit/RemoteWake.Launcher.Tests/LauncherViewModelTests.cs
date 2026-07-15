using RemoteWake.Application.Models;
using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Results;
using RemoteWake.Domain.Wake;
using RemoteWake.Launcher.Wpf.Services;
using RemoteWake.Launcher.Wpf.ViewModels;

namespace RemoteWake.Launcher.Tests;

[TestClass]
public sealed class LauncherViewModelTests
{
    [TestMethod]
    public void UnconfiguredDashboardExplainsWhyStartIsBlocked()
    {
        using var viewModel = new LauncherViewModel(
            new FakeLauncherService(),
            new KeyTextProvider(),
            null,
            "PC principal");

        Assert.IsTrue(viewModel.IsDashboardVisible);
        Assert.IsFalse(viewModel.CanStart);
        Assert.AreEqual("StatusConfigurationPending", viewModel.ComputerStatus);
        Assert.AreEqual("StartBlockedReason", viewModel.StartBlockedReason);
        Assert.IsFalse(viewModel.StartCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task SuccessfulFlowShowsCompletedTimelineAndSuccessScreen()
    {
        var correlationId = CorrelationId.New();
        var service = new FakeLauncherService
        {
            Handler = (profile, progress, cancellationToken) =>
            {
                progress.Report(new WakeProgressUpdate(correlationId, WakeState.SendingWake, DateTimeOffset.UtcNow));
                progress.Report(new WakeProgressUpdate(correlationId, WakeState.WaitingWindows, DateTimeOffset.UtcNow));
                return ValueTask.FromResult(new WakeExecutionResult(
                    WakeState.Completed,
                    [
                        WakeState.Checking,
                        WakeState.SendingWake,
                        WakeState.WaitingWindows,
                        WakeState.WaitingService,
                        WakeState.OpeningClient,
                        WakeState.Completed,
                    ],
                    null,
                    correlationId,
                    TimeSpan.FromSeconds(4)));
            },
        };
        using var viewModel = CreateConfiguredViewModel(service);

        await viewModel.StartAsync();

        Assert.IsTrue(viewModel.IsSuccessVisible);
        Assert.AreEqual("StepCompleted", viewModel.RequestStepStatus);
        Assert.AreEqual("StepCompleted", viewModel.ComputerStepStatus);
        Assert.AreEqual("StepCompleted", viewModel.WindowsStepStatus);
        Assert.AreEqual("StepCompleted", viewModel.ClientStepStatus);
        Assert.AreEqual(correlationId.ToString(), viewModel.CorrelationIdText);
        Assert.IsFalse(viewModel.IsBusy);
    }

    [TestMethod]
    public async Task BridgeFailureIsActionableAndDoesNotExposeTechnicalReason()
    {
        var correlationId = CorrelationId.New();
        var service = new FakeLauncherService
        {
            Handler = (profile, progress, cancellationToken) =>
                ValueTask.FromResult(new WakeExecutionResult(
                    WakeState.Failed,
                    [WakeState.Checking, WakeState.BridgeUnavailable, WakeState.Failed],
                    DomainError.Create(ErrorCode.ERR009, "ssh_connection_refused:203.0.113.42"),
                    correlationId,
                    TimeSpan.FromSeconds(1))),
        };
        using var viewModel = CreateConfiguredViewModel(service);

        await viewModel.StartAsync();

        Assert.IsTrue(viewModel.IsFailureVisible);
        Assert.AreEqual("ErrorBridgeTitle", viewModel.ErrorTitle);
        Assert.AreEqual("ErrorBridgeAction", viewModel.ErrorAction);
        Assert.AreEqual("ERR009", viewModel.ErrorCodeText);
        Assert.IsFalse(viewModel.ErrorTitle.Contains("203.0.113.42", StringComparison.Ordinal));
        Assert.IsFalse(viewModel.ErrorConsequence.Contains("ssh", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task CancelStopsActiveOperationAndReturnsToDashboard()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new FakeLauncherService
        {
            Handler = async (profile, progress, cancellationToken) =>
            {
                var correlationId = CorrelationId.New();
                progress.Report(new WakeProgressUpdate(correlationId, WakeState.WaitingWindows, DateTimeOffset.UtcNow));
                entered.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return new WakeExecutionResult(
                    WakeState.Completed,
                    [WakeState.Checking, WakeState.Completed],
                    null,
                    correlationId,
                    TimeSpan.FromMilliseconds(20));
            },
        };
        using var viewModel = CreateConfiguredViewModel(service);

        var operation = viewModel.StartAsync();
        await entered.Task;
        viewModel.Cancel();
        await operation;

        Assert.IsTrue(viewModel.IsDashboardVisible);
        Assert.IsFalse(viewModel.IsBusy);
        Assert.AreEqual(1, service.Calls);
    }

    [TestMethod]
    public void IdentityMismatchMapsToSafeManualRepairAction()
    {
        var mapper = new ErrorPresentationMapper(new KeyTextProvider());

        var presentation = mapper.Map(ErrorCode.ERR010);

        Assert.AreEqual("ErrorIdentityTitle", presentation.Title);
        Assert.AreEqual("ErrorIdentityAction", presentation.Action);
        Assert.AreEqual("ErrorConsequence", presentation.Consequence);
    }

    private static LauncherViewModel CreateConfiguredViewModel(FakeLauncherService service) =>
        new(service, new KeyTextProvider(), CreateProfile(), "PC principal", isDemo: true);

    private static WakeProfile CreateProfile() =>
        new(ComputerId.New(), BridgeId.New(), TargetId.New(), "rustdesk");

    private sealed class KeyTextProvider : ILauncherTextProvider
    {
        public string GetText(string resourceKey) => resourceKey;
    }

    private sealed class FakeLauncherService : IWakeLauncherService
    {
        public Func<WakeProfile, IProgress<WakeProgressUpdate>, CancellationToken, ValueTask<WakeExecutionResult>>?
            Handler
        { get; init; }

        public int Calls { get; private set; }

        public ValueTask<WakeExecutionResult> ExecuteAsync(
            WakeProfile profile,
            IProgress<WakeProgressUpdate> progress,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Handler?.Invoke(profile, progress, cancellationToken)
                ?? throw new InvalidOperationException("No fake result was configured.");
        }
    }
}
