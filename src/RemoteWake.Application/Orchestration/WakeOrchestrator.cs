using RemoteWake.Application.Models;
using RemoteWake.Application.Observability;
using RemoteWake.Application.Ports;
using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Results;
using RemoteWake.Domain.Security;
using RemoteWake.Domain.Time;
using RemoteWake.Domain.Wake;

namespace RemoteWake.Application.Orchestration;

public sealed class WakeOrchestrator
{
    private readonly IVpnAdapter vpnAdapter;
    private readonly IBridgeClient bridgeClient;
    private readonly IWakeStateProbe wakeStateProbe;
    private readonly IRemoteAppAdapter remoteAppAdapter;
    private readonly IAuditSink auditSink;
    private readonly IClock clock;
    private readonly INonceGenerator nonceGenerator;
    private readonly IBackoffPolicy backoffPolicy;
    private readonly WakeOrchestratorOptions options;

    public WakeOrchestrator(
        IVpnAdapter vpnAdapter,
        IBridgeClient bridgeClient,
        IWakeStateProbe wakeStateProbe,
        IRemoteAppAdapter remoteAppAdapter,
        IAuditSink auditSink,
        IClock clock,
        INonceGenerator nonceGenerator,
        IBackoffPolicy backoffPolicy,
        WakeOrchestratorOptions? options = null)
    {
        this.vpnAdapter = vpnAdapter ?? throw new ArgumentNullException(nameof(vpnAdapter));
        this.bridgeClient = bridgeClient ?? throw new ArgumentNullException(nameof(bridgeClient));
        this.wakeStateProbe = wakeStateProbe ?? throw new ArgumentNullException(nameof(wakeStateProbe));
        this.remoteAppAdapter = remoteAppAdapter ?? throw new ArgumentNullException(nameof(remoteAppAdapter));
        this.auditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        this.nonceGenerator = nonceGenerator ?? throw new ArgumentNullException(nameof(nonceGenerator));
        this.backoffPolicy = backoffPolicy ?? throw new ArgumentNullException(nameof(backoffPolicy));
        this.options = options ?? new WakeOrchestratorOptions();
        this.options.Validate();
    }

    public async ValueTask<WakeExecutionResult> ExecuteAsync(
        WakeProfile profile,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var execution = new ExecutionContext(CorrelationId.New(), clock.UtcNow);

        try
        {
            return await ExecuteCoreAsync(profile, execution, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return await CancelAsync(execution).ConfigureAwait(false);
        }
    }

    private async ValueTask<WakeExecutionResult> ExecuteCoreAsync(
        WakeProfile profile,
        ExecutionContext execution,
        CancellationToken cancellationToken)
    {
        var windows = await wakeStateProbe
            .ProbeWindowsAsync(profile.ComputerId, cancellationToken)
            .ConfigureAwait(false);

        if (windows.IsFailure)
        {
            return await FailAsync(execution, windows.Error!, cancellationToken).ConfigureAwait(false);
        }

        return windows.Value.IsReady
            ? await HandleReadyComputerAsync(profile, execution, cancellationToken).ConfigureAwait(false)
            : await HandleWakeRequiredAsync(profile, execution, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<WakeExecutionResult> HandleReadyComputerAsync(
        WakeProfile profile,
        ExecutionContext execution,
        CancellationToken cancellationToken)
    {
        var transition = await TransitionAsync(
            execution,
            WakeTrigger.PcAlreadyReady,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (transition.IsFailure)
        {
            return await FailAsync(execution, transition.Error!, cancellationToken).ConfigureAwait(false);
        }

        var service = await wakeStateProbe
            .ProbeRemoteServiceAsync(profile.ComputerId, profile.RequestedServiceId, cancellationToken)
            .ConfigureAwait(false);

        if (service.IsFailure)
        {
            return await FailAsync(execution, service.Error!, cancellationToken).ConfigureAwait(false);
        }

        if (!service.Value.IsReady)
        {
            var waitingForService = await TransitionAsync(
                execution,
                WakeTrigger.ServicePending,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (waitingForService.IsFailure)
            {
                return await FailAsync(execution, waitingForService.Error!, cancellationToken).ConfigureAwait(false);
            }

            var waitResult = await WaitForServiceAsync(profile, cancellationToken).ConfigureAwait(false);
            if (waitResult.IsFailure)
            {
                return await FailAsync(execution, waitResult.Error!, cancellationToken).ConfigureAwait(false);
            }
        }

        return await OpenClientAsync(profile, execution, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<WakeExecutionResult> HandleWakeRequiredAsync(
        WakeProfile profile,
        ExecutionContext execution,
        CancellationToken cancellationToken)
    {
        var bridgeStatus = await GetAuthenticatedBridgeAsync(profile, cancellationToken).ConfigureAwait(false);
        if (bridgeStatus.IsFailure)
        {
            await TransitionAsync(
                execution,
                WakeTrigger.BridgeUnavailable,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return await FailAsync(execution, bridgeStatus.Error!, cancellationToken).ConfigureAwait(false);
        }

        var sendingWake = await TransitionAsync(
            execution,
            WakeTrigger.WakeRequired,
            new WakeTransitionContext(
                PcIsReady: false,
                BridgeIsAuthenticatedAndHealthy: bridgeStatus.Value.CanWake),
            cancellationToken).ConfigureAwait(false);

        if (sendingWake.IsFailure)
        {
            return await FailAsync(execution, sendingWake.Error!, cancellationToken).ConfigureAwait(false);
        }

        var requestId = RequestId.New();
        var command = new WakeCommand(requestId, profile.TargetId, clock.UtcNow, nonceGenerator.Create());
        var receipt = await bridgeClient
            .SendWakeAsync(profile.BridgeId, command, cancellationToken)
            .ConfigureAwait(false);

        if (receipt.IsFailure || !receipt.Value.IsAccepted)
        {
            var error = receipt.Error ?? DomainError.Create(ErrorCode.ERR012, "wake_rejected");
            return await FailAsync(execution, error, cancellationToken).ConfigureAwait(false);
        }

        var waitingForWindows = await TransitionAsync(
            execution,
            WakeTrigger.WakeAccepted,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (waitingForWindows.IsFailure)
        {
            return await FailAsync(execution, waitingForWindows.Error!, cancellationToken).ConfigureAwait(false);
        }

        var windows = await WaitForWindowsAsync(profile, cancellationToken).ConfigureAwait(false);
        if (windows.IsFailure)
        {
            return await FailAsync(execution, windows.Error!, cancellationToken).ConfigureAwait(false);
        }

        var waitingForService = await TransitionAsync(
            execution,
            WakeTrigger.WindowsReady,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (waitingForService.IsFailure)
        {
            return await FailAsync(execution, waitingForService.Error!, cancellationToken).ConfigureAwait(false);
        }

        var service = await WaitForServiceAsync(profile, cancellationToken).ConfigureAwait(false);
        if (service.IsFailure)
        {
            return await FailAsync(execution, service.Error!, cancellationToken).ConfigureAwait(false);
        }

        return await OpenClientAsync(profile, execution, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<Result<BridgeHealth>> GetAuthenticatedBridgeAsync(
        WakeProfile profile,
        CancellationToken cancellationToken)
    {
        var vpn = await vpnAdapter
            .GetStatusAsync(profile.BridgeId, cancellationToken)
            .ConfigureAwait(false);

        if (vpn.IsFailure)
        {
            return Result.Failure<BridgeHealth>(vpn.Error!);
        }

        if (!vpn.Value.IsConnected)
        {
            return Result.Failure<BridgeHealth>(DomainError.Create(ErrorCode.ERR008, "vpn_disconnected", true));
        }

        var health = await bridgeClient
            .GetHealthAsync(profile.BridgeId, cancellationToken)
            .ConfigureAwait(false);

        if (health.IsFailure)
        {
            return health;
        }

        return health.Value.CanWake
            ? health
            : Result.Failure<BridgeHealth>(DomainError.Create(ErrorCode.ERR009, "bridge_not_healthy", true));
    }

    private async ValueTask<Result> WaitForWindowsAsync(
        WakeProfile profile,
        CancellationToken cancellationToken)
    {
        var deadline = clock.UtcNow + options.WindowsTimeout;
        var attempt = 0;

        while (clock.UtcNow < deadline)
        {
            await DelayUntilNextProbeAsync(attempt++, deadline, cancellationToken).ConfigureAwait(false);
            var probe = await wakeStateProbe
                .ProbeWindowsAsync(profile.ComputerId, cancellationToken)
                .ConfigureAwait(false);

            if (probe.IsSuccess && probe.Value.IsReady)
            {
                return Result.Success();
            }

            if (probe.IsFailure && !probe.Error!.IsRetryable)
            {
                return Result.Failure(probe.Error);
            }
        }

        return Result.Failure(DomainError.Create(ErrorCode.ERR014, "windows_timeout", true));
    }

    private async ValueTask<Result> WaitForServiceAsync(
        WakeProfile profile,
        CancellationToken cancellationToken)
    {
        var deadline = clock.UtcNow + options.ServiceTimeout;
        var attempt = 0;

        while (clock.UtcNow < deadline)
        {
            await DelayUntilNextProbeAsync(attempt++, deadline, cancellationToken).ConfigureAwait(false);
            var probe = await wakeStateProbe
                .ProbeRemoteServiceAsync(
                    profile.ComputerId,
                    profile.RequestedServiceId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (probe.IsSuccess && probe.Value.IsReady)
            {
                return Result.Success();
            }

            if (probe.IsFailure && !probe.Error!.IsRetryable)
            {
                return Result.Failure(probe.Error);
            }
        }

        return Result.Failure(DomainError.Create(ErrorCode.ERR015, "service_timeout", true));
    }

    private async ValueTask DelayUntilNextProbeAsync(
        int attempt,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        var remaining = deadline - clock.UtcNow;
        var requested = backoffPolicy.GetDelay(attempt);
        var delay = requested <= remaining ? requested : remaining;

        if (delay > TimeSpan.Zero)
        {
            await clock.DelayAsync(delay, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask<WakeExecutionResult> OpenClientAsync(
        WakeProfile profile,
        ExecutionContext execution,
        CancellationToken cancellationToken)
    {
        var openingClient = await TransitionAsync(
            execution,
            WakeTrigger.ServiceReady,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (openingClient.IsFailure)
        {
            return await FailAsync(execution, openingClient.Error!, cancellationToken).ConfigureAwait(false);
        }

        var launch = await remoteAppAdapter.LaunchAsync(profile, cancellationToken).ConfigureAwait(false);
        if (launch.IsFailure || !launch.Value.Started)
        {
            var error = launch.Error ?? DomainError.Create(ErrorCode.ERR016, "client_not_started");
            return await FailAsync(execution, error, cancellationToken).ConfigureAwait(false);
        }

        var completed = await TransitionAsync(
            execution,
            WakeTrigger.ClientOpened,
            new WakeTransitionContext(LaunchSucceeded: true),
            cancellationToken).ConfigureAwait(false);

        return completed.IsSuccess
            ? CreateExecutionResult(execution, null)
            : await FailAsync(execution, completed.Error!, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<Result<WakeTransition>> TransitionAsync(
        ExecutionContext execution,
        WakeTrigger trigger,
        WakeTransitionContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var transition = execution.StateMachine.TryTransition(trigger, context);
        if (transition.IsFailure)
        {
            return transition;
        }

        execution.StateHistory.Add(transition.Value.Current);
        var logEvent = LogSanitizer.CreateTransitionEvent(clock.UtcNow, execution.CorrelationId, transition.Value);
        await auditSink.WriteAsync(logEvent, cancellationToken).ConfigureAwait(false);
        return transition;
    }

    private async ValueTask<WakeExecutionResult> FailAsync(
        ExecutionContext execution,
        DomainError error,
        CancellationToken cancellationToken)
    {
        if (execution.StateMachine.Current is not WakeState.Failed)
        {
            await TransitionAsync(
                execution,
                WakeTrigger.Fail,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        var logEvent = LogSanitizer.CreateFailureEvent(
            clock.UtcNow,
            execution.CorrelationId,
            error,
            execution.StateMachine.Current);
        await auditSink.WriteAsync(logEvent, cancellationToken).ConfigureAwait(false);
        return CreateExecutionResult(execution, error);
    }

    private async ValueTask<WakeExecutionResult> CancelAsync(ExecutionContext execution)
    {
        await TransitionAsync(execution, WakeTrigger.Cancel).ConfigureAwait(false);
        return CreateExecutionResult(execution, null);
    }

    private WakeExecutionResult CreateExecutionResult(ExecutionContext execution, DomainError? error) =>
        new(
            execution.StateMachine.Current,
            execution.StateHistory.AsReadOnly(),
            error,
            execution.CorrelationId,
            clock.UtcNow - execution.StartedAt);

    private sealed class ExecutionContext
    {
        public ExecutionContext(CorrelationId correlationId, DateTimeOffset startedAt)
        {
            CorrelationId = correlationId;
            StartedAt = startedAt;
            StateHistory.Add(WakeState.Checking);
        }

        public CorrelationId CorrelationId { get; }

        public DateTimeOffset StartedAt { get; }

        public WakeStateMachine StateMachine { get; } = new();

        public List<WakeState> StateHistory { get; } = [];
    }
}
