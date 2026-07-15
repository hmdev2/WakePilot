using System.ComponentModel;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using RemoteWake.Application.Models;
using RemoteWake.Application.Ports;
using RemoteWake.Domain.Results;

namespace RemoteWake.Infrastructure.Windows.Security;

[SupportedOSPlatform("windows")]
public sealed class DpapiSecretVault : ISecretVault
{
    private const string ReferencePrefix = "dpapi:";
    private readonly string vaultDirectory;

    public DpapiSecretVault(string vaultDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vaultDirectory);
        if (!Path.IsPathFullyQualified(vaultDirectory))
        {
            throw new ArgumentException("Vault directory must be absolute.", nameof(vaultDirectory));
        }

        this.vaultDirectory = Path.GetFullPath(vaultDirectory);
    }

    public async ValueTask<Result<SecretReference>> StoreAsync(
        string purpose,
        ReadOnlyMemory<byte> secret,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        if (secret.IsEmpty || secret.Length > 65_536)
        {
            throw new ArgumentException("Secret must contain at most 65536 bytes.", nameof(secret));
        }

        cancellationToken.ThrowIfCancellationRequested();
        byte[]? protectedBytes = null;
        try
        {
            protectedBytes = WindowsDpapi.Protect(secret.Span);
            Directory.CreateDirectory(vaultDirectory);
            var id = Guid.NewGuid();
            var destination = GetPath(id);
            await WriteAtomicallyAsync(destination, protectedBytes, cancellationToken).ConfigureAwait(false);
            return Result.Success(new SecretReference(ReferencePrefix + id.ToString("D")));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CryptographicException or Win32Exception)
        {
            return Failure<SecretReference>("Secret could not be protected by the Windows user profile.");
        }
        finally
        {
            if (protectedBytes is not null)
            {
                CryptographicOperations.ZeroMemory(protectedBytes);
            }
        }
    }

    public async ValueTask<Result<ReadOnlyMemory<byte>>> RetrieveAsync(
        SecretReference reference,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reference);
        if (!TryParseReference(reference, out var id))
        {
            return Failure<ReadOnlyMemory<byte>>("Secret reference is invalid.");
        }

        byte[]? protectedBytes = null;
        try
        {
            protectedBytes = await File.ReadAllBytesAsync(GetPath(id), cancellationToken).ConfigureAwait(false);
            if (protectedBytes.Length is 0 or > 131_072)
            {
                return Failure<ReadOnlyMemory<byte>>("Protected secret size is invalid.");
            }

            var plaintext = WindowsDpapi.Unprotect(protectedBytes);
            return Result.Success<ReadOnlyMemory<byte>>(plaintext);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CryptographicException or Win32Exception)
        {
            return Failure<ReadOnlyMemory<byte>>("Secret could not be recovered from the Windows user profile.");
        }
        finally
        {
            if (protectedBytes is not null)
            {
                CryptographicOperations.ZeroMemory(protectedBytes);
            }
        }
    }

    public ValueTask<Result> RemoveAsync(
        SecretReference reference,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reference);
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryParseReference(reference, out var id))
        {
            return ValueTask.FromResult(Result.Failure(DomainError.Create(
                ErrorCode.ERR006,
                "Secret reference is invalid.")));
        }

        try
        {
            File.Delete(GetPath(id));
            return ValueTask.FromResult(Result.Success());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return ValueTask.FromResult(Result.Failure(DomainError.Create(
                ErrorCode.ERR006,
                "Secret could not be removed from the Windows user profile.")));
        }
    }

    private string GetPath(Guid id) => Path.Combine(vaultDirectory, id.ToString("N") + ".dpapi");

    private static bool TryParseReference(SecretReference reference, out Guid id)
    {
        id = Guid.Empty;
        var value = reference.Name;
        return value is not null &&
            value.StartsWith(ReferencePrefix, StringComparison.Ordinal) &&
            Guid.TryParseExact(value[ReferencePrefix.Length..], "D", out id) &&
            id != Guid.Empty;
    }

    private static async Task WriteAtomicallyAsync(
        string destination,
        byte[] content,
        CancellationToken cancellationToken)
    {
        var temporary = destination + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            await File.WriteAllBytesAsync(temporary, content, cancellationToken).ConfigureAwait(false);
            File.Move(temporary, destination);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    private static Result<T> Failure<T>(string reason) =>
        Result.Failure<T>(DomainError.Create(ErrorCode.ERR006, reason));
}
