using System.Security.Cryptography;
using RemoteWake.Application.Models;
using RemoteWake.Application.Ports;
using RemoteWake.Domain.Results;

namespace RemoteWake.Infrastructure.RemoteApps;

public sealed class RustDeskRemoteAppAdapter : IRemoteAppAdapter
{
    private readonly RemoteAppLaunchOptions options;
    private readonly IRemoteProcessStarter processStarter;

    public RustDeskRemoteAppAdapter(
        RemoteAppLaunchOptions options,
        IRemoteProcessStarter processStarter)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.processStarter = processStarter ?? throw new ArgumentNullException(nameof(processStarter));
    }

    public async ValueTask<Result<LaunchResult>> LaunchAsync(
        WakeProfile profile,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(profile.RequestedServiceId, "rustdesk", StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(options.ExecutablePath))
        {
            return LaunchFailure("rustdesk_profile_invalid");
        }

        try
        {
            var currentHash = await ComputeSha256Async(options.ExecutablePath, cancellationToken).ConfigureAwait(false);
            if (!CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(currentHash),
                    Convert.FromHexString(options.ExpectedSha256)))
            {
                return LaunchFailure("rustdesk_binary_changed");
            }

            var request = new RemoteProcessStartRequest(options.ExecutablePath, []);
            var started = await processStarter.StartAsync(request, cancellationToken).ConfigureAwait(false);
            return started
                ? Result.Success(new LaunchResult(true))
                : LaunchFailure("rustdesk_start_rejected");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return LaunchFailure("rustdesk_launch_failed");
        }
    }

    private static async ValueTask<string> ComputeSha256Async(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash);
    }

    private static Result<LaunchResult> LaunchFailure(string reason) =>
        Result.Failure<LaunchResult>(DomainError.Create(ErrorCode.ERR016, reason, true));
}
