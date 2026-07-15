namespace RemoteWake.Infrastructure.RemoteApps;

public sealed record RemoteAppLaunchOptions
{
    public RemoteAppLaunchOptions(string executablePath, string expectedSha256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedSha256);

        if (!Path.IsPathFullyQualified(executablePath) ||
            !string.Equals(Path.GetFullPath(executablePath), executablePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Executable path must be absolute and canonical.", nameof(executablePath));
        }

        if (!string.Equals(Path.GetExtension(executablePath), ".exe", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Remote application must be a Windows executable.", nameof(executablePath));
        }

        if (expectedSha256.Length != 64 || !expectedSha256.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("Expected SHA-256 must contain 64 hexadecimal characters.", nameof(expectedSha256));
        }

        ExecutablePath = executablePath;
        ExpectedSha256 = expectedSha256.ToUpperInvariant();
    }

    public string ExecutablePath { get; }

    public string ExpectedSha256 { get; }
}
