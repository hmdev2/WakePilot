using System.Security.Cryptography;

namespace RemoteWake.M1.Harness;

internal sealed record HostKeyPin(string KeyType, string EncodedKey, string Fingerprint)
{
    public static HostKeyPin Parse(string publicKey, string expectedFingerprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedFingerprint);

        var parts = publicKey.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 2 or > 3 || !string.Equals(parts[0], "ssh-ed25519", StringComparison.Ordinal))
        {
            throw new InvalidDataException("A chave pública do host deve ser Ed25519.");
        }

        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(parts[1]);
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("A chave pública do host é inválida.", exception);
        }

        if (decoded.Length < 32 ||
            !string.Equals(Convert.ToBase64String(decoded), parts[1], StringComparison.Ordinal))
        {
            throw new InvalidDataException("A chave pública do host não usa codificação canônica.");
        }

        var fingerprint = "SHA256:" + Convert.ToBase64String(SHA256.HashData(decoded)).TrimEnd('=');
        if (!CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.ASCII.GetBytes(fingerprint),
            System.Text.Encoding.ASCII.GetBytes(expectedFingerprint)))
        {
            throw new InvalidDataException("A identidade SSH informada não corresponde ao fingerprint confirmado.");
        }

        return new HostKeyPin(parts[0], parts[1], fingerprint);
    }

    public string CreateKnownHostsEntry(string host, ushort port)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        var destination = port == 22 ? host : $"[{host}]:{port}";
        return $"{destination} {KeyType} {EncodedKey}{Environment.NewLine}";
    }
}
