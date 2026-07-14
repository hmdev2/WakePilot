using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace RemoteWake.Infrastructure.Tailscale;

internal static class TailscaleStatusParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = false,
    };

    public static TailscalePeerState Parse(string json, string expectedNodeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedNodeName);

        var status = JsonSerializer.Deserialize<TailscaleStatusDocument>(json, SerializerOptions)
            ?? throw new JsonException("Tailscale status is empty.");

        if (!string.Equals(status.BackendState, "Running", StringComparison.Ordinal))
        {
            return new TailscalePeerState(false, false, null);
        }

        var peer = status.Peer?.Values.FirstOrDefault(candidate =>
            IsExpectedNode(candidate.HostName, expectedNodeName) ||
            IsExpectedNode(candidate.DnsName, expectedNodeName));

        if (peer is null)
        {
            return new TailscalePeerState(true, false, null);
        }

        var address = peer.TailscaleIPs?
            .FirstOrDefault(value =>
                IPAddress.TryParse(value, out var parsed) &&
                parsed.AddressFamily == AddressFamily.InterNetwork);

        return new TailscalePeerState(true, peer.Online, address);
    }

    private static bool IsExpectedNode(string? actual, string expected) =>
        actual is not null &&
        string.Equals(actual.TrimEnd('.'), expected.TrimEnd('.'), StringComparison.OrdinalIgnoreCase);

    private sealed record TailscaleStatusDocument
    {
        public string? BackendState { get; init; }

        public Dictionary<string, TailscalePeerDocument>? Peer { get; init; }
    }

    private sealed record TailscalePeerDocument
    {
        public string? HostName { get; init; }

        public string? DnsName { get; init; }

        public string[]? TailscaleIPs { get; init; }

        public bool Online { get; init; }
    }
}

internal sealed record TailscalePeerState(bool IsRunning, bool IsPeerOnline, string? PeerAddress);
