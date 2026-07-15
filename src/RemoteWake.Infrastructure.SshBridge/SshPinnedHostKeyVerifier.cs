using System.Security.Cryptography;
using RemoteWake.Application.Models;
using RemoteWake.Application.Ports;
using RemoteWake.Domain.Results;

namespace RemoteWake.Infrastructure.SshBridge;

public interface ISshHostKeyVerifier
{
    ValueTask<Result> VerifyAsync(
        SshBridgeEndpoint endpoint,
        CancellationToken cancellationToken);
}

public sealed class SshPinnedHostKeyVerifier : ISshHostKeyVerifier
{
    private const int MaximumProbeOutputCharacters = 4096;
    private readonly IProcessRunner processRunner;
    private readonly SshBridgeOptions options;

    public SshPinnedHostKeyVerifier(IProcessRunner processRunner, SshBridgeOptions options)
    {
        this.processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async ValueTask<Result> VerifyAsync(
        SshBridgeEndpoint endpoint,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        try
        {
            var pinnedKey = ReadPinnedKey(endpoint);
            var observedHostsPath = Path.GetFullPath(Path.Combine(
                Path.GetTempPath(),
                "wakepilot-host-probe-" + Guid.NewGuid().ToString("N")));
            try
            {
                _ = await processRunner
                    .RunAsync(CreateInvocation(endpoint, observedHostsPath), cancellationToken)
                    .ConfigureAwait(false);

                PinnedKey observedKey;
                try
                {
                    observedKey = ReadObservedKey(endpoint, observedHostsPath);
                }
                catch (InvalidDataException)
                {
                    return Unreachable("SSH host-key probe returned no canonical Ed25519 key.");
                }

                return KeysEqual(pinnedKey, observedKey)
                    ? Result.Success()
                    : Result.Failure(DomainError.Create(
                        ErrorCode.ERR010,
                        "SSH host identity does not match the pinned key."));
            }
            finally
            {
                File.Delete(observedHostsPath);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TimeoutException)
        {
            return Unreachable("SSH host-key probe timed out.");
        }
        catch (InvalidDataException)
        {
            return Result.Failure(DomainError.Create(
                ErrorCode.ERR010,
                "Pinned SSH host identity is invalid."));
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            return Unreachable("SSH host-key probe could not read the pinned identity or endpoint.");
        }
    }

    public ProcessInvocation CreateInvocation(
        SshBridgeEndpoint endpoint,
        string observedHostsFilePath)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(observedHostsFilePath);
        if (!Path.IsPathFullyQualified(observedHostsFilePath) ||
            !string.Equals(
                Path.GetFullPath(observedHostsFilePath),
                observedHostsFilePath,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Observed SSH host-key path must be absolute and canonical.",
                nameof(observedHostsFilePath));
        }

        return new ProcessInvocation(
            options.ExecutablePath,
            [
                "-F", "none",
                "-T",
                "-n",
                "-S", "none",
                "-o", "BatchMode=yes",
                "-o", "StrictHostKeyChecking=accept-new",
                "-o", "CheckHostIP=no",
                "-o", $"UserKnownHostsFile={observedHostsFilePath}",
                "-o", "GlobalKnownHostsFile=none",
                "-o", "IdentitiesOnly=yes",
                "-o", "IdentityAgent=none",
                "-o", "PubkeyAuthentication=no",
                "-o", "PasswordAuthentication=no",
                "-o", "KbdInteractiveAuthentication=no",
                "-o", "PreferredAuthentications=none",
                "-o", "ForwardAgent=no",
                "-o", "ForwardX11=no",
                "-o", "ClearAllForwardings=yes",
                "-o", "PermitLocalCommand=no",
                "-o", "RequestTTY=no",
                "-o", "ControlMaster=no",
                "-o", "ConnectTimeout=5",
                "-p", endpoint.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
                $"{endpoint.UserName}@{endpoint.Host}",
            ],
            options.CommandTimeout,
            MaximumProbeOutputCharacters,
            completeWhenFileContainsData: observedHostsFilePath);
    }

    private PinnedKey ReadPinnedKey(SshBridgeEndpoint endpoint)
    {
        return ReadSingleKey(endpoint, options.KnownHostsFilePath, "Pinned");
    }

    private static PinnedKey ReadObservedKey(
        SshBridgeEndpoint endpoint,
        string observedHostsFilePath)
    {
        return ReadSingleKey(endpoint, observedHostsFilePath, "Observed");
    }

    private static PinnedKey ReadSingleKey(
        SshBridgeEndpoint endpoint,
        string path,
        string source)
    {
        var content = File.ReadAllBytes(path);
        if (content.Length is 0 or > 16_384)
        {
            throw new InvalidDataException($"{source} SSH host identity has an invalid size.");
        }

        var expectedDestination = endpoint.Port == 22
            ? endpoint.Host
            : $"[{endpoint.Host}]:{endpoint.Port}";
        var matches = new List<PinnedKey>();
        foreach (var line in System.Text.Encoding.ASCII.GetString(content)
                     .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 3 &&
                string.Equals(parts[0], expectedDestination, StringComparison.Ordinal) &&
                string.Equals(parts[1], "ssh-ed25519", StringComparison.Ordinal))
            {
                matches.Add(ParseKey(parts[1], parts[2]));
            }
        }

        return matches.Count == 1
            ? matches[0]
            : throw new InvalidDataException($"{source} SSH host identity is missing or ambiguous.");
    }

    private static PinnedKey ParseKey(string keyType, string encodedKey)
    {
        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(encodedKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("SSH host public key is not valid base64.", exception);
        }

        if (decoded.Length < 32 ||
            !string.Equals(Convert.ToBase64String(decoded), encodedKey, StringComparison.Ordinal))
        {
            throw new InvalidDataException("SSH host public key is not canonical.");
        }

        return new PinnedKey(keyType, decoded);
    }

    private static bool KeysEqual(PinnedKey left, PinnedKey right) =>
        string.Equals(left.KeyType, right.KeyType, StringComparison.Ordinal) &&
        left.DecodedKey.Length == right.DecodedKey.Length &&
        CryptographicOperations.FixedTimeEquals(left.DecodedKey, right.DecodedKey);

    private static Result Unreachable(string reason) => Result.Failure(DomainError.Create(
        ErrorCode.ERR009,
        reason,
        isRetryable: true));

    private sealed record PinnedKey(string KeyType, byte[] DecodedKey);
}
