using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        var peers = status.Peer?.Values.Where(candidate =>
            IsExpectedNode(candidate.HostName, expectedNodeName) ||
            IsExpectedNode(candidate.DnsName, expectedNodeName)).ToArray() ?? [];

        if (peers.Length == 0)
        {
            return new TailscalePeerState(true, false, null);
        }

        if (peers.Length > 1)
        {
            throw new InvalidDataException("Tailscale node identity is ambiguous.");
        }

        var peer = peers[0];

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

        [JsonPropertyName("DNSName")]
        public string? DnsName { get; init; }

        public string[]? TailscaleIPs { get; init; }

        public bool Online { get; init; }
    }
}

internal sealed record TailscalePeerState(bool IsRunning, bool IsPeerOnline, string? PeerAddress);
