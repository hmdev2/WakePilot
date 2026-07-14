using System.Globalization;
using System.Text;
using System.Text.Json;
using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Security;

namespace RemoteWake.Infrastructure.SshBridge;

public enum BridgeAction
{
    Health,
    Wake,
}

public enum BridgeResponseStatus
{
    Accepted,
    Rejected,
    Error,
}

public sealed record BridgeProtocolRequest(
    RequestId RequestId,
    TargetId TargetId,
    DateTimeOffset IssuedAt,
    Nonce Nonce,
    BridgeAction Action);

public sealed record BridgeProtocolResponse(
    RequestId RequestId,
    BridgeResponseStatus Status,
    string Code,
    int PacketCount,
    DateTimeOffset ServerTime);

public static class BridgeProtocolCodec
{
    public const int Version = 1;
    public const int MaximumPayloadBytes = 4096;
    public const string EnvelopePrefix = "rwa1:";

    private static readonly HashSet<string> RequestProperties =
        new(StringComparer.Ordinal) { "v", "requestId", "targetId", "issuedAt", "nonce", "action" };
    private static readonly HashSet<string> ResponseProperties =
        new(StringComparer.Ordinal) { "v", "requestId", "status", "code", "packetCount", "serverTime" };

    public static string EncodeRequest(BridgeProtocolRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("v", Version);
            writer.WriteString("requestId", request.RequestId.ToString());
            writer.WriteString("targetId", request.TargetId.ToString());
            writer.WriteString("issuedAt", request.IssuedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
            writer.WriteString("nonce", request.Nonce.RevealForTransport());
            writer.WriteString("action", request.Action == BridgeAction.Health ? "health" : "wake");
            writer.WriteEndObject();
        }

        if (stream.Length > MaximumPayloadBytes)
        {
            throw new InvalidDataException("Bridge request exceeds the protocol size limit.");
        }

        return EnvelopePrefix + ToBase64Url(stream.ToArray());
    }

    public static BridgeProtocolRequest DecodeRequest(string envelope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(envelope);
        if (!envelope.StartsWith(EnvelopePrefix, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Bridge request envelope is invalid.");
        }

        var bytes = FromBase64Url(envelope[EnvelopePrefix.Length..]);
        using var document = ParseClosedObject(bytes, RequestProperties);
        var root = document.RootElement;
        ValidateVersion(root);

        return new BridgeProtocolRequest(
            RequestId.From(ReadGuid(root, "requestId")),
            TargetId.From(ReadGuid(root, "targetId")),
            ReadUtcTimestamp(root, "issuedAt"),
            Nonce.Parse(ReadRequiredString(root, "nonce")),
            ReadAction(root));
    }

    public static BridgeProtocolResponse DecodeResponse(string json, RequestId expectedRequestId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ArgumentNullException.ThrowIfNull(expectedRequestId);

        var bytes = Encoding.UTF8.GetBytes(json.TrimEnd('\r', '\n'));
        using var document = ParseClosedObject(bytes, ResponseProperties);
        var root = document.RootElement;
        ValidateVersion(root);

        var requestId = RequestId.From(ReadGuid(root, "requestId"));
        if (requestId != expectedRequestId)
        {
            throw new InvalidDataException("Bridge response correlation does not match the request.");
        }

        var status = ReadRequiredString(root, "status") switch
        {
            "accepted" => BridgeResponseStatus.Accepted,
            "rejected" => BridgeResponseStatus.Rejected,
            "error" => BridgeResponseStatus.Error,
            _ => throw new InvalidDataException("Bridge response status is invalid."),
        };

        var code = ReadRequiredString(root, "code");
        if (!(string.Equals(code, "OK", StringComparison.Ordinal) ||
            (code.Length == 6 && code.StartsWith("ERR", StringComparison.Ordinal) && code[3..].All(char.IsAsciiDigit))))
        {
            throw new InvalidDataException("Bridge response code is invalid.");
        }

        var packetCountElement = root.GetProperty("packetCount");
        if (!packetCountElement.TryGetInt32(out var packetCount) || packetCount is < 0 or > 3)
        {
            throw new InvalidDataException("Bridge packet count is invalid.");
        }

        return new BridgeProtocolResponse(
            requestId,
            status,
            code,
            packetCount,
            ReadUtcTimestamp(root, "serverTime"));
    }

    private static JsonDocument ParseClosedObject(byte[] bytes, HashSet<string> expectedProperties)
    {
        if (bytes.Length is 0 or > MaximumPayloadBytes)
        {
            throw new InvalidDataException("Bridge payload size is invalid.");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(bytes, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 4,
            });
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Bridge payload is not valid JSON.", exception);
        }

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            document.Dispose();
            throw new InvalidDataException("Bridge payload must be a JSON object.");
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!names.Add(property.Name) || !expectedProperties.Contains(property.Name))
            {
                document.Dispose();
                throw new InvalidDataException("Bridge payload contains duplicate or unknown fields.");
            }
        }

        if (!names.SetEquals(expectedProperties))
        {
            document.Dispose();
            throw new InvalidDataException("Bridge payload is missing required fields.");
        }

        return document;
    }

    private static void ValidateVersion(JsonElement root)
    {
        if (!root.GetProperty("v").TryGetInt32(out var version) || version != Version)
        {
            throw new InvalidDataException("Bridge protocol version is incompatible.");
        }
    }

    private static Guid ReadGuid(JsonElement root, string propertyName)
    {
        var value = ReadRequiredString(root, propertyName);
        return Guid.TryParseExact(value, "D", out var parsed) && parsed != Guid.Empty
            ? parsed
            : throw new InvalidDataException($"Bridge {propertyName} is invalid.");
    }

    private static DateTimeOffset ReadUtcTimestamp(JsonElement root, string propertyName)
    {
        var value = ReadRequiredString(root, propertyName);
        if (!DateTimeOffset.TryParseExact(
            value,
            "O",
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var parsed) || parsed.Offset != TimeSpan.Zero)
        {
            throw new InvalidDataException($"Bridge {propertyName} must be an exact UTC timestamp.");
        }

        return parsed;
    }

    private static string ReadRequiredString(JsonElement root, string propertyName)
    {
        var element = root.GetProperty(propertyName);
        var value = element.ValueKind == JsonValueKind.String ? element.GetString() : null;
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidDataException($"Bridge {propertyName} is invalid.");
    }

    private static BridgeAction ReadAction(JsonElement root) =>
        ReadRequiredString(root, "action") switch
        {
            "health" => BridgeAction.Health,
            "wake" => BridgeAction.Wake,
            _ => throw new InvalidDataException("Bridge action is invalid."),
        };

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string encoded)
    {
        var maximumEncodedLength = ((MaximumPayloadBytes + 2) / 3) * 4;
        if (encoded.Length == 0 || encoded.Length > maximumEncodedLength || encoded.Any(character =>
            !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_')))
        {
            throw new InvalidDataException("Bridge request is not valid base64url.");
        }

        var padded = encoded.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - (padded.Length % 4)) % 4);

        try
        {
            var bytes = Convert.FromBase64String(padded);
            if (bytes.Length > MaximumPayloadBytes)
            {
                throw new InvalidDataException("Bridge payload exceeds the protocol size limit.");
            }

            if (!string.Equals(ToBase64Url(bytes), encoded, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Bridge request base64url is not canonical.");
            }

            return bytes;
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("Bridge request is not valid base64url.", exception);
        }
    }
}
