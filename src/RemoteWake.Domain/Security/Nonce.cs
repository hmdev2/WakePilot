using System.Security.Cryptography;

namespace RemoteWake.Domain.Security;

public readonly record struct Nonce
{
    private const int ByteLength = 32;
    private const int EncodedLength = 43;

    private Nonce(string value)
    {
        Value = value;
    }

    private string Value { get; }

    public static Nonce Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length != EncodedLength || !value.All(IsBase64UrlCharacter))
        {
            throw new FormatException("Nonce must be a 32-byte base64url value without padding.");
        }

        return new Nonce(value);
    }

    public string RevealForTransport() => Value;

    public override string ToString() => "[REDACTED]";

    internal static Nonce CreateRandom()
    {
        Span<byte> bytes = stackalloc byte[ByteLength];
        RandomNumberGenerator.Fill(bytes);
        var encoded = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return new Nonce(encoded);
    }

    private static bool IsBase64UrlCharacter(char character) =>
        char.IsAsciiLetterOrDigit(character) || character is '-' or '_';
}

public interface INonceGenerator
{
    Nonce Create();
}

public sealed class CryptographicNonceGenerator : INonceGenerator
{
    public Nonce Create() => Nonce.CreateRandom();
}
