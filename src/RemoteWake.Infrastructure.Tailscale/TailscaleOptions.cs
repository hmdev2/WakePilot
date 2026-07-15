using RemoteWake.Domain.Identifiers;

namespace RemoteWake.Infrastructure.Tailscale;

public sealed record TailscaleOptions
{
    public TailscaleOptions(
        string executablePath,
        IReadOnlyDictionary<BridgeId, string> bridgeNodeNames,
        TimeSpan? commandTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(bridgeNodeNames);

        ExecutablePath = executablePath;
        BridgeNodeNames = bridgeNodeNames.ToDictionary(
            pair => pair.Key ?? throw new ArgumentException("Bridge identifier cannot be null.", nameof(bridgeNodeNames)),
            pair => ValidateNodeName(pair.Value));
        CommandTimeout = commandTimeout ?? TimeSpan.FromSeconds(5);

        if (CommandTimeout <= TimeSpan.Zero || CommandTimeout > TimeSpan.FromSeconds(30))
        {
            throw new ArgumentOutOfRangeException(
                nameof(commandTimeout),
                "Tailscale command timeout must be between zero and 30 seconds.");
        }
    }

    public string ExecutablePath { get; }

    public IReadOnlyDictionary<BridgeId, string> BridgeNodeNames { get; }

    public TimeSpan CommandTimeout { get; }

    private static string ValidateNodeName(string nodeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);
        var trimmed = nodeName.TrimEnd('.');

        if (trimmed.Length > 253 ||
            trimmed.StartsWith('-') ||
            trimmed.Any(character =>
                !(char.IsAsciiLetterOrDigit(character) || character is '-' or '.')))
        {
            throw new ArgumentException("Tailscale node name is invalid.", nameof(nodeName));
        }

        return trimmed;
    }
}
