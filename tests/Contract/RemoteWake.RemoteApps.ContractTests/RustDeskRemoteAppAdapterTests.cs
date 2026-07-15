using System.Security.Cryptography;
using RemoteWake.Application.Models;
using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Results;
using RemoteWake.Infrastructure.RemoteApps;

namespace RemoteWake.RemoteApps.ContractTests;

[TestClass]
public sealed class RustDeskRemoteAppAdapterTests
{
    private readonly string temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        "WakePilot.RemoteApps.Tests",
        Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [TestMethod]
    public async Task ValidPinnedRustDeskBinaryStartsWithoutArgumentsOrShell()
    {
        var executablePath = CreateExecutableFixture([0x4D, 0x5A, 0x01, 0x02]);
        var starter = new RecordingProcessStarter();
        var adapter = new RustDeskRemoteAppAdapter(
            new RemoteAppLaunchOptions(executablePath, ComputeSha256(executablePath)),
            starter);

        var result = await adapter.LaunchAsync(CreateProfile(), CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Value.Started);
        Assert.IsNotNull(starter.Request);
        Assert.AreEqual(executablePath, starter.Request.ExecutablePath);
        Assert.IsEmpty(starter.Request.Arguments);

        var startInfo = SystemRemoteProcessStarter.CreateStartInfo(starter.Request);
        Assert.IsFalse(startInfo.UseShellExecute);
        Assert.AreEqual(string.Empty, startInfo.Arguments);
        Assert.IsEmpty(startInfo.ArgumentList);
        Assert.AreEqual(Path.GetDirectoryName(executablePath), startInfo.WorkingDirectory);
    }

    [TestMethod]
    public async Task ChangedBinaryIsRejectedBeforeProcessStart()
    {
        var executablePath = CreateExecutableFixture([0x4D, 0x5A, 0x01]);
        var expectedHash = ComputeSha256(executablePath);
        File.WriteAllBytes(executablePath, [0x4D, 0x5A, 0x02]);
        var starter = new RecordingProcessStarter();
        var adapter = new RustDeskRemoteAppAdapter(
            new RemoteAppLaunchOptions(executablePath, expectedHash),
            starter);

        var result = await adapter.LaunchAsync(CreateProfile(), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ErrorCode.ERR016, result.Error!.Code);
        Assert.IsNull(starter.Request);
    }

    [TestMethod]
    public async Task MissingExecutableIsRejectedBeforeProcessStart()
    {
        Directory.CreateDirectory(temporaryDirectory);
        var executablePath = Path.Combine(temporaryDirectory, "rustdesk.exe");
        var starter = new RecordingProcessStarter();
        var adapter = new RustDeskRemoteAppAdapter(
            new RemoteAppLaunchOptions(executablePath, new string('A', 64)),
            starter);

        var result = await adapter.LaunchAsync(CreateProfile(), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ErrorCode.ERR016, result.Error!.Code);
        Assert.IsNull(starter.Request);
    }

    [TestMethod]
    public async Task DifferentServiceProfileCannotReuseRustDeskAdapter()
    {
        var executablePath = CreateExecutableFixture([0x4D, 0x5A]);
        var starter = new RecordingProcessStarter();
        var adapter = new RustDeskRemoteAppAdapter(
            new RemoteAppLaunchOptions(executablePath, ComputeSha256(executablePath)),
            starter);
        var profile = new WakeProfile(ComputerId.New(), BridgeId.New(), TargetId.New(), "custom");

        var result = await adapter.LaunchAsync(profile, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ErrorCode.ERR016, result.Error!.Code);
        Assert.IsNull(starter.Request);
    }

    [TestMethod]
    public async Task ProcessStarterRejectionReturnsTypedLaunchFailure()
    {
        var executablePath = CreateExecutableFixture([0x4D, 0x5A]);
        var starter = new RecordingProcessStarter { StartResult = false };
        var adapter = new RustDeskRemoteAppAdapter(
            new RemoteAppLaunchOptions(executablePath, ComputeSha256(executablePath)),
            starter);

        var result = await adapter.LaunchAsync(CreateProfile(), CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(ErrorCode.ERR016, result.Error!.Code);
        Assert.IsNotNull(starter.Request);
    }

    [TestMethod]
    public void LaunchOptionsRejectNonCanonicalPathAndInvalidHash()
    {
        Directory.CreateDirectory(temporaryDirectory);
        var nonCanonicalPath = Path.Combine(temporaryDirectory, ".", "rustdesk.exe");

        Assert.ThrowsExactly<ArgumentException>(() =>
            new RemoteAppLaunchOptions(nonCanonicalPath, new string('A', 64)));
        Assert.ThrowsExactly<ArgumentException>(() =>
            new RemoteAppLaunchOptions(Path.Combine(temporaryDirectory, "rustdesk.exe"), "not-a-hash"));
    }

    private string CreateExecutableFixture(byte[] contents)
    {
        Directory.CreateDirectory(temporaryDirectory);
        var path = Path.Combine(temporaryDirectory, "rustdesk.exe");
        File.WriteAllBytes(path, contents);
        return path;
    }

    private static string ComputeSha256(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static WakeProfile CreateProfile() =>
        new(ComputerId.New(), BridgeId.New(), TargetId.New(), "rustdesk");

    private sealed class RecordingProcessStarter : IRemoteProcessStarter
    {
        public bool StartResult { get; init; } = true;

        public RemoteProcessStartRequest? Request { get; private set; }

        public ValueTask<bool> StartAsync(
            RemoteProcessStartRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Request = request;
            return ValueTask.FromResult(StartResult);
        }
    }
}
