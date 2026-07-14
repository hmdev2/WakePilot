using System.Diagnostics;
using System.Text;
using RemoteWake.Application.Models;
using RemoteWake.Application.Ports;

namespace RemoteWake.Infrastructure.Processes;

public sealed class SystemProcessRunner : IProcessRunner
{
    public async ValueTask<ProcessExecutionResult> RunAsync(
        ProcessInvocation invocation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        if (!File.Exists(invocation.ExecutablePath))
        {
            throw new FileNotFoundException("The configured executable was not found.", invocation.ExecutablePath);
        }

        using var process = new Process
        {
            StartInfo = CreateStartInfo(invocation),
            EnableRaisingEvents = true,
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("The configured process could not be started.");
        }

        using var timeout = new CancellationTokenSource(invocation.Timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        try
        {
            var standardOutput = ReadLimitedAsync(
                process.StandardOutput,
                invocation.MaximumOutputCharacters,
                linked.Token);
            var standardError = ReadLimitedAsync(
                process.StandardError,
                invocation.MaximumOutputCharacters,
                linked.Token);

            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
            return new ProcessExecutionResult(
                process.ExitCode,
                await standardOutput.ConfigureAwait(false),
                await standardError.ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            throw new TimeoutException("The configured process exceeded its execution timeout.");
        }
        catch
        {
            TryKill(process);
            throw;
        }
    }

    internal static ProcessStartInfo CreateStartInfo(ProcessInvocation invocation)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = invocation.ExecutablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            CreateNoWindow = true,
        };

        foreach (var argument in invocation.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static async Task<string> ReadLimitedAsync(
        StreamReader reader,
        int maximumCharacters,
        CancellationToken cancellationToken)
    {
        var buffer = new char[Math.Min(1024, maximumCharacters + 1)];
        var output = new StringBuilder(Math.Min(maximumCharacters, 4096));

        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return output.ToString();
            }

            if (output.Length + read > maximumCharacters)
            {
                throw new InvalidDataException("Process output exceeded the configured limit.");
            }

            output.Append(buffer, 0, read);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process exited between the state check and the kill request.
        }
    }
}
