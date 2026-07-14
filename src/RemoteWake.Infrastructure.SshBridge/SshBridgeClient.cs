using RemoteWake.Application.Models;
using RemoteWake.Application.Ports;
using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Results;
using RemoteWake.Domain.Security;
using RemoteWake.Domain.Time;

namespace RemoteWake.Infrastructure.SshBridge;

public sealed class SshBridgeClient : IBridgeClient
{
    private readonly IProcessRunner processRunner;
    private readonly SshBridgeOptions options;
    private readonly IClock clock;
    private readonly INonceGenerator nonceGenerator;

    public SshBridgeClient(
        IProcessRunner processRunner,
        SshBridgeOptions options,
        IClock clock,
        INonceGenerator nonceGenerator)
    {
        this.processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        this.nonceGenerator = nonceGenerator ?? throw new ArgumentNullException(nameof(nonceGenerator));
    }

    public async ValueTask<Result<BridgeHealth>> GetHealthAsync(
        BridgeId bridgeId,
        CancellationToken cancellationToken)
    {
        if (!TryGetEndpoint(bridgeId, out var endpoint, out var failure))
        {
            return Result.Failure<BridgeHealth>(failure!);
        }

        var response = await ExecuteAsync(
            endpoint!,
            new BridgeProtocolRequest(
                RequestId.New(),
                endpoint!.TargetId,
                clock.UtcNow,
                nonceGenerator.Create(),
                BridgeAction.Health),
            cancellationToken).ConfigureAwait(false);

        return response.IsSuccess
            ? Result.Success(new BridgeHealth(IsAccepted(response.Value), IsAccepted(response.Value)))
            : Result.Failure<BridgeHealth>(response.Error!);
    }

    public async ValueTask<Result<WakeReceipt>> SendWakeAsync(
        BridgeId bridgeId,
        WakeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!TryGetEndpoint(bridgeId, out var endpoint, out var failure))
        {
            return Result.Failure<WakeReceipt>(failure!);
        }

        if (command.TargetId != endpoint!.TargetId)
        {
            return Result.Failure<WakeReceipt>(DomainError.Create(
                ErrorCode.ERR012,
                "Wake target is not allowlisted for the selected bridge."));
        }

        var response = await ExecuteAsync(
            endpoint,
            new BridgeProtocolRequest(
                command.RequestId,
                command.TargetId,
                command.IssuedAt,
                command.Nonce,
                BridgeAction.Wake),
            cancellationToken).ConfigureAwait(false);

        if (response.IsFailure)
        {
            return Result.Failure<WakeReceipt>(response.Error!);
        }

        if (!IsAccepted(response.Value) || response.Value.PacketCount != 3)
        {
            return Result.Failure<WakeReceipt>(CreateResponseError(response.Value));
        }

        return Result.Success(new WakeReceipt(command.RequestId, true, response.Value.PacketCount));
    }

    public ProcessInvocation CreateInvocation(SshBridgeEndpoint endpoint, BridgeProtocolRequest request)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(request);

        var arguments = new List<string>
        {
            "-F", "none",
            "-T",
            "-S", "none",
            "-o", "BatchMode=yes",
            "-o", "StrictHostKeyChecking=yes",
            "-o", "CheckHostIP=yes",
            "-o", $"UserKnownHostsFile={options.KnownHostsFilePath}",
            "-o", "GlobalKnownHostsFile=none",
            "-o", "IdentitiesOnly=yes",
            "-o", "IdentityAgent=none",
            "-o", "PasswordAuthentication=no",
            "-o", "KbdInteractiveAuthentication=no",
            "-o", "PreferredAuthentications=publickey",
            "-o", "ForwardAgent=no",
            "-o", "ForwardX11=no",
            "-o", "ClearAllForwardings=yes",
            "-o", "PermitLocalCommand=no",
            "-o", "RequestTTY=no",
            "-o", "ControlMaster=no",
            "-o", "ConnectTimeout=5",
            "-i", options.IdentityFilePath,
            "-p", endpoint.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
            $"{endpoint.UserName}@{endpoint.Host}",
            BridgeProtocolCodec.EncodeRequest(request),
        };

        return new ProcessInvocation(
            options.ExecutablePath,
            arguments,
            options.CommandTimeout,
            BridgeProtocolCodec.MaximumPayloadBytes);
    }

    private async ValueTask<Result<BridgeProtocolResponse>> ExecuteAsync(
        SshBridgeEndpoint endpoint,
        BridgeProtocolRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var process = await processRunner
                .RunAsync(CreateInvocation(endpoint, request), cancellationToken)
                .ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var identityMismatch = process.ExitCode == 255 &&
                    (process.StandardError.Contains("REMOTE HOST IDENTIFICATION HAS CHANGED", StringComparison.Ordinal) ||
                     process.StandardError.Contains("Host key verification failed", StringComparison.OrdinalIgnoreCase));
                return Result.Failure<BridgeProtocolResponse>(DomainError.Create(
                    identityMismatch ? ErrorCode.ERR010 : ErrorCode.ERR009,
                    identityMismatch
                        ? "SSH host identity does not match the pinned key."
                        : "SSH bridge command failed.",
                    isRetryable: !identityMismatch));
            }

            var response = BridgeProtocolCodec.DecodeResponse(process.StandardOutput, request.RequestId);
            return Result.Success(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is InvalidDataException or IOException or TimeoutException)
        {
            return Result.Failure<BridgeProtocolResponse>(DomainError.Create(
                ErrorCode.ERR009,
                "SSH bridge response could not be read safely.",
                isRetryable: true));
        }
    }

    private bool TryGetEndpoint(
        BridgeId bridgeId,
        out SshBridgeEndpoint? endpoint,
        out DomainError? failure)
    {
        ArgumentNullException.ThrowIfNull(bridgeId);
        if (options.Endpoints.TryGetValue(bridgeId, out endpoint))
        {
            failure = null;
            return true;
        }

        failure = DomainError.Create(ErrorCode.ERR009, "Bridge does not have a configured SSH endpoint.");
        return false;
    }

    private static bool IsAccepted(BridgeProtocolResponse response) =>
        response.Status == BridgeResponseStatus.Accepted &&
        string.Equals(response.Code, "OK", StringComparison.Ordinal);

    private static DomainError CreateResponseError(BridgeProtocolResponse response)
    {
        var code = Enum.TryParse<ErrorCode>(response.Code, ignoreCase: false, out var parsed)
            ? parsed
            : ErrorCode.ERR012;
        return DomainError.Create(code, "Bridge rejected or did not acknowledge the wake request.");
    }
}
