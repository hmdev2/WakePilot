using System.Text.Json;
using RemoteWake.Application.Models;
using RemoteWake.Application.Ports;
using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Results;

namespace RemoteWake.Infrastructure.Tailscale;

public sealed class TailscaleVpnAdapter : IVpnAdapter
{
    private readonly IProcessRunner processRunner;
    private readonly TailscaleOptions options;

    public TailscaleVpnAdapter(IProcessRunner processRunner, TailscaleOptions options)
    {
        this.processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async ValueTask<Result<VpnStatus>> GetStatusAsync(
        BridgeId bridgeId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bridgeId);

        if (!options.BridgeNodeNames.TryGetValue(bridgeId, out var nodeName))
        {
            return Failure("Bridge does not have a configured Tailscale node.");
        }

        try
        {
            var invocation = new ProcessInvocation(
                options.ExecutablePath,
                ["status", "--json"],
                options.CommandTimeout,
                maximumOutputCharacters: 65_536);
            var process = await processRunner.RunAsync(invocation, cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                return Failure("Tailscale status command failed.");
            }

            var state = TailscaleStatusParser.Parse(process.StandardOutput, nodeName);
            return Result.Success(new VpnStatus(
                state.IsRunning,
                state.PeerAddress,
                state.IsPeerOnline));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is JsonException or InvalidDataException or IOException or TimeoutException)
        {
            return Failure("Tailscale status could not be read safely.");
        }
    }

    private static Result<VpnStatus> Failure(string reason) =>
        Result.Failure<VpnStatus>(DomainError.Create(ErrorCode.ERR008, reason, isRetryable: true));
}
