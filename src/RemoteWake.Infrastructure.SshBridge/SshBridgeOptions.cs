using RemoteWake.Domain.Identifiers;

namespace RemoteWake.Infrastructure.SshBridge;

public sealed record SshBridgeEndpoint
{
    public SshBridgeEndpoint(
        string host,
        ushort port,
        string userName,
        TargetId targetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentNullException.ThrowIfNull(targetId);

        if (host.Length > 253 || host.StartsWith('-') || host.Any(character =>
            !(char.IsAsciiLetterOrDigit(character) || character is '-' or '.')))
        {
            throw new ArgumentException("SSH host is invalid.", nameof(host));
        }

        if (userName.Length > 64 || userName.StartsWith('-') || userName.Any(character =>
            !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_')))
        {
            throw new ArgumentException("SSH user name is invalid.", nameof(userName));
        }

        Host = host;
        Port = port;
        UserName = userName;
        TargetId = targetId;
    }

    public string Host { get; }

    public ushort Port { get; }

    public string UserName { get; }

    public TargetId TargetId { get; }
}

public sealed record SshBridgeOptions
{
    public SshBridgeOptions(
        string executablePath,
        string identityFilePath,
        string knownHostsFilePath,
        IReadOnlyDictionary<BridgeId, SshBridgeEndpoint> endpoints,
        TimeSpan? commandTimeout = null)
    {
        ExecutablePath = ValidateCanonicalPath(executablePath, nameof(executablePath));
        IdentityFilePath = ValidateCanonicalPath(identityFilePath, nameof(identityFilePath));
        KnownHostsFilePath = ValidateCanonicalPath(knownHostsFilePath, nameof(knownHostsFilePath));
        ArgumentNullException.ThrowIfNull(endpoints);
        Endpoints = endpoints.ToDictionary(
            pair => pair.Key ?? throw new ArgumentException("Bridge identifier cannot be null.", nameof(endpoints)),
            pair => pair.Value ?? throw new ArgumentException("SSH endpoint cannot be null.", nameof(endpoints)));
        CommandTimeout = commandTimeout ?? TimeSpan.FromSeconds(10);

        if (CommandTimeout <= TimeSpan.Zero || CommandTimeout > TimeSpan.FromSeconds(30))
        {
            throw new ArgumentOutOfRangeException(
                nameof(commandTimeout),
                "SSH command timeout must be between zero and 30 seconds.");
        }
    }

    public string ExecutablePath { get; }

    public string IdentityFilePath { get; }

    public string KnownHostsFilePath { get; }

    public IReadOnlyDictionary<BridgeId, SshBridgeEndpoint> Endpoints { get; }

    public TimeSpan CommandTimeout { get; }

    private static string ValidateCanonicalPath(string path, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, parameterName);
        if (!Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException("SSH paths must be absolute.", parameterName);
        }

        var fullPath = Path.GetFullPath(path);
        return string.Equals(fullPath, path, StringComparison.OrdinalIgnoreCase)
            ? fullPath
            : throw new ArgumentException("SSH paths must be canonical.", parameterName);
    }
}
