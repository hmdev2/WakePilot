using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using RemoteWake.Application.Models;
using RemoteWake.Application.Ports;
using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Results;

namespace RemoteWake.Infrastructure.Windows.Security;

[SupportedOSPlatform("windows")]
public sealed class DpapiPrivateKeyLeaseProvider : IPrivateKeyLeaseProvider
{
    private readonly ISecretVault vault;
    private readonly Dictionary<BridgeId, SecretReference> references;
    private readonly string leaseDirectory;

    public DpapiPrivateKeyLeaseProvider(
        ISecretVault vault,
        IReadOnlyDictionary<BridgeId, SecretReference> references,
        string leaseDirectory)
    {
        this.vault = vault ?? throw new ArgumentNullException(nameof(vault));
        ArgumentNullException.ThrowIfNull(references);
        this.references = references.ToDictionary(pair => pair.Key, pair => pair.Value);
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseDirectory);
        if (!Path.IsPathFullyQualified(leaseDirectory))
        {
            throw new ArgumentException("Lease directory must be absolute.", nameof(leaseDirectory));
        }

        this.leaseDirectory = Path.GetFullPath(leaseDirectory);
    }

    public async ValueTask<Result<IPrivateKeyLease>> AcquireAsync(
        BridgeId bridgeId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bridgeId);
        if (!references.TryGetValue(bridgeId, out var reference))
        {
            return Failure("Bridge does not have a protected private key reference.");
        }

        var secret = await vault.RetrieveAsync(reference, cancellationToken).ConfigureAwait(false);
        if (secret.IsFailure)
        {
            return Result.Failure<IPrivateKeyLease>(secret.Error!);
        }

        var plaintext = secret.Value.ToArray();
        if (MemoryMarshal.TryGetArray(secret.Value, out var secretBuffer) && secretBuffer.Array is not null)
        {
            CryptographicOperations.ZeroMemory(secretBuffer.AsSpan());
        }

        string? createdPath = null;
        var leaseCreated = false;
        try
        {
            Directory.CreateDirectory(leaseDirectory);
            ApplyRestrictedDirectoryAcl(leaseDirectory);
            CleanupAbandonedLeases();
            createdPath = Path.Combine(leaseDirectory, "identity-" + Guid.NewGuid().ToString("N"));
            await using (var stream = new FileStream(
                createdPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough | FileOptions.Asynchronous))
            {
                ApplyRestrictedFileAcl(createdPath);
                await stream.WriteAsync(plaintext, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            leaseCreated = true;
            return Result.Success<IPrivateKeyLease>(new TemporaryPrivateKeyLease(createdPath));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Failure("A temporary private key lease could not be created safely.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            if (!leaseCreated && createdPath is not null)
            {
                File.Delete(createdPath);
            }
        }
    }

    private void CleanupAbandonedLeases()
    {
        var cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(5);
        foreach (var path in Directory.EnumerateFiles(leaseDirectory, "identity-*", SearchOption.TopDirectoryOnly))
        {
            if (File.GetLastWriteTimeUtc(path) < cutoff)
            {
                File.Delete(path);
            }
        }
    }

    private static void ApplyRestrictedDirectoryAcl(string path)
    {
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        foreach (var rule in CreateAccessRules(inherit: true))
        {
            security.AddAccessRule(rule);
        }

        new DirectoryInfo(path).SetAccessControl(security);
    }

    private static void ApplyRestrictedFileAcl(string path)
    {
        var security = new FileSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        foreach (var rule in CreateAccessRules(inherit: false))
        {
            security.AddAccessRule(rule);
        }

        new FileInfo(path).SetAccessControl(security);
    }

    private static IEnumerable<FileSystemAccessRule> CreateAccessRules(bool inherit)
    {
        using var identity = WindowsIdentity.GetCurrent();
        var user = identity.User ?? throw new InvalidOperationException("Current Windows user SID is unavailable.");
        var inheritance = inherit
            ? InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit
            : InheritanceFlags.None;
        yield return new FileSystemAccessRule(
            user,
            FileSystemRights.FullControl,
            inheritance,
            PropagationFlags.None,
            AccessControlType.Allow);
        yield return new FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            FileSystemRights.FullControl,
            inheritance,
            PropagationFlags.None,
            AccessControlType.Allow);
        yield return new FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
            FileSystemRights.FullControl,
            inheritance,
            PropagationFlags.None,
            AccessControlType.Allow);
    }

    private static Result<IPrivateKeyLease> Failure(string reason) =>
        Result.Failure<IPrivateKeyLease>(DomainError.Create(ErrorCode.ERR006, reason));

    private sealed class TemporaryPrivateKeyLease(string filePath) : IPrivateKeyLease
    {
        private int disposed;

        public string FilePath { get; } = filePath;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                File.Delete(FilePath);
            }

            return ValueTask.CompletedTask;
        }
    }
}
