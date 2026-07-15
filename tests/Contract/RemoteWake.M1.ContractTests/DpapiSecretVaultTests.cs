using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using RemoteWake.Application.Models;
using RemoteWake.Application.Ports;
using RemoteWake.Domain.Identifiers;
using RemoteWake.Infrastructure.Windows.Security;

namespace RemoteWake.M1.ContractTests;

[TestClass]
public sealed class DpapiSecretVaultTests
{
    [TestMethod]
    public async Task Ct011PrivateMaterialIsDpapiProtectedAtRestAndRemovable()
    {
        using var directory = new TemporaryDirectory();
        var vault = new DpapiSecretVault(directory.Path);
        var plaintext = "fixture-private-material-not-a-real-key"u8.ToArray();

        var stored = await vault.StoreAsync("ssh-identity", plaintext, CancellationToken.None);

        Assert.IsTrue(stored.IsSuccess);
        var encryptedPath = Directory.GetFiles(directory.Path, "*.dpapi").Single();
        var protectedBytes = await File.ReadAllBytesAsync(encryptedPath);
        Assert.IsFalse(protectedBytes.AsSpan().IndexOf(plaintext) >= 0);
        Assert.IsFalse(protectedBytes.SequenceEqual(plaintext));

        var retrieved = await vault.RetrieveAsync(stored.Value, CancellationToken.None);
        Assert.IsTrue(retrieved.IsSuccess);
        CollectionAssert.AreEqual(plaintext, retrieved.Value.ToArray());

        var removed = await vault.RemoveAsync(stored.Value, CancellationToken.None);
        Assert.IsTrue(removed.IsSuccess);
        Assert.IsFalse(File.Exists(encryptedPath));
        CryptographicOperations.ZeroMemory(plaintext);
    }

    [TestMethod]
    public async Task Ct011SshIdentityExistsOnlyForLeaseLifetime()
    {
        using var directory = new TemporaryDirectory();
        var vaultPath = System.IO.Path.Combine(directory.Path, "vault");
        var leasePath = System.IO.Path.Combine(directory.Path, "leases");
        var vault = new DpapiSecretVault(vaultPath);
        var bridgeId = BridgeId.From(Guid.Parse("77777777-7777-4777-8777-777777777777"));
        var privateMaterial = "fixture-ephemeral-private-material"u8.ToArray();
        var stored = await vault.StoreAsync("ssh-identity", privateMaterial, CancellationToken.None);
        Assert.IsTrue(stored.IsSuccess);
        var provider = new DpapiPrivateKeyLeaseProvider(
            vault,
            new Dictionary<BridgeId, SecretReference> { [bridgeId] = stored.Value },
            leasePath);

        var acquired = await provider.AcquireAsync(bridgeId, CancellationToken.None);

        Assert.IsTrue(acquired.IsSuccess);
        var leasedFile = acquired.Value.FilePath;
        Assert.IsTrue(File.Exists(leasedFile));
        CollectionAssert.AreEqual(privateMaterial, await File.ReadAllBytesAsync(leasedFile));
        var rules = new FileInfo(leasedFile)
            .GetAccessControl()
            .GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier));
        Assert.AreEqual(3, rules.Count);
        Assert.IsFalse(rules.Cast<FileSystemAccessRule>().Any(rule => rule.IsInherited));
        await acquired.Value.DisposeAsync();
        Assert.IsFalse(File.Exists(leasedFile));
        CryptographicOperations.ZeroMemory(privateMaterial);
    }

    [TestMethod]
    public async Task Ct011CorruptedOrOversizedProtectedBlobFailsClosed()
    {
        using var directory = new TemporaryDirectory();
        var vault = new DpapiSecretVault(directory.Path);
        var stored = await vault.StoreAsync("ssh-identity", "fixture"u8.ToArray(), CancellationToken.None);
        Assert.IsTrue(stored.IsSuccess);
        var encryptedPath = Directory.GetFiles(directory.Path, "*.dpapi").Single();
        await File.WriteAllBytesAsync(encryptedPath, new byte[131_073]);

        var retrieved = await vault.RetrieveAsync(stored.Value, CancellationToken.None);

        Assert.IsTrue(retrieved.IsFailure);
        Assert.AreEqual(RemoteWake.Domain.Results.ErrorCode.ERR006, retrieved.Error!.Code);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RemoteWake.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
