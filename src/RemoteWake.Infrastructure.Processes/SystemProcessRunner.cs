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

        process.StandardInput.Close();

        using var timeout = new CancellationTokenSource(invocation.Timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        try
        {
            if (invocation.CompleteOnFirstOutputLine)
            {
                return await ReadSingleLineResponseAsync(process, invocation, linked.Token).ConfigureAwait(false);
            }

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

    public static ProcessStartInfo CreateStartInfo(ProcessInvocation invocation)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = invocation.ExecutablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
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

    private static async Task<ProcessExecutionResult> ReadSingleLineResponseAsync(
        Process process,
        ProcessInvocation invocation,
        CancellationToken cancellationToken)
    {
        var standardError = ReadLimitedAsync(
            process.StandardError,
            invocation.MaximumOutputCharacters,
            cancellationToken);
        var firstLine = ReadFirstLineLimitedAsync(
            process.StandardOutput,
            invocation.MaximumOutputCharacters,
            cancellationToken);
        var processExit = process.WaitForExitAsync(cancellationToken);
        var completed = await Task.WhenAny(firstLine, processExit).ConfigureAwait(false);

        if (completed == firstLine && await firstLine.ConfigureAwait(false) is { } responseLine)
        {
            TryKill(process);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            var remainingBudget = invocation.MaximumOutputCharacters - responseLine.Length;
            var remaining = await ReadLimitedAsync(
                process.StandardOutput,
                remainingBudget,
                CancellationToken.None).ConfigureAwait(false);
            var output = remaining.Length == 0
                ? responseLine
                : responseLine + Environment.NewLine + remaining;
            return new ProcessExecutionResult(
                0,
                output,
                await standardError.ConfigureAwait(false));
        }

        await processExit.ConfigureAwait(false);
        var line = await firstLine.ConfigureAwait(false);
        var trailing = await ReadLimitedAsync(
            process.StandardOutput,
            invocation.MaximumOutputCharacters - (line?.Length ?? 0),
            CancellationToken.None).ConfigureAwait(false);
        var standardOutput = line is null
            ? trailing
            : trailing.Length == 0
                ? line
                : line + Environment.NewLine + trailing;
        return new ProcessExecutionResult(
            process.ExitCode,
            standardOutput,
            await standardError.ConfigureAwait(false));
    }

    private static async Task<string?> ReadFirstLineLimitedAsync(
        StreamReader reader,
        int maximumCharacters,
        CancellationToken cancellationToken)
    {
        var output = new StringBuilder(Math.Min(maximumCharacters, 4096));
        var buffer = new char[1];
        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return output.Length == 0 ? null : output.ToString().TrimEnd('\r');
            }

            if (buffer[0] == '\n')
            {
                return output.ToString().TrimEnd('\r');
            }

            output.Append(buffer[0]);
            if (output.Length > maximumCharacters)
            {
                throw new InvalidDataException("Process output exceeded the configured limit.");
            }
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
