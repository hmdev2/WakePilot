using System.Diagnostics;

namespace RemoteWake.Infrastructure.RemoteApps;

public sealed record RemoteProcessStartRequest
{
    public RemoteProcessStartRequest(string executablePath, IEnumerable<string> arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(arguments);

        if (!Path.IsPathFullyQualified(executablePath) ||
            !string.Equals(Path.GetFullPath(executablePath), executablePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Executable path must be absolute and canonical.", nameof(executablePath));
        }

        ExecutablePath = executablePath;
        Arguments = arguments.Select(argument =>
        {
            ArgumentNullException.ThrowIfNull(argument);
            return argument;
        }).ToArray();
    }

    public string ExecutablePath { get; }

    public IReadOnlyList<string> Arguments { get; }
}

public interface IRemoteProcessStarter
{
    ValueTask<bool> StartAsync(
        RemoteProcessStartRequest request,
        CancellationToken cancellationToken);
}

public sealed class SystemRemoteProcessStarter : IRemoteProcessStarter
{
    public ValueTask<bool> StartAsync(
        RemoteProcessStartRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        using var process = new Process { StartInfo = CreateStartInfo(request) };
        return ValueTask.FromResult(process.Start());
    }

    public static ProcessStartInfo CreateStartInfo(RemoteProcessStartRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var startInfo = new ProcessStartInfo
        {
            FileName = request.ExecutablePath,
            WorkingDirectory = Path.GetDirectoryName(request.ExecutablePath),
            UseShellExecute = false,
            CreateNoWindow = false,
        };

        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }
}
