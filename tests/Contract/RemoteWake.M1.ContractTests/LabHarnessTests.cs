using RemoteWake.Application.Models;
using RemoteWake.Domain.Identifiers;
using RemoteWake.M1.Harness;

namespace RemoteWake.M1.ContractTests;

[TestClass]
public sealed class LabHarnessTests
{
    private const string HostKey = "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIF97N8ItHz5XLJ8vvcVtLJZs9qPZ7nYPQJ9jCLr2Tf1d android";
    private const string Fingerprint = "SHA256:Bw356WYWl5DygvT/hFnhh2sTNfop9e9i3CV/Yq73IGo";

    [TestMethod]
    public void HostPinRequiresConfirmedEd25519Fingerprint()
    {
        var pin = HostKeyPin.Parse(HostKey, Fingerprint);

        Assert.AreEqual(Fingerprint, pin.Fingerprint);
        Assert.AreEqual("[100.64.0.10]:8023 ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIF97N8ItHz5XLJ8vvcVtLJZs9qPZ7nYPQJ9jCLr2Tf1d\r\n",
            pin.CreateKnownHostsEntry("100.64.0.10", 8023));
        Assert.Throws<InvalidDataException>(() => HostKeyPin.Parse(HostKey, "SHA256:divergente"));
        Assert.Throws<InvalidDataException>(() => HostKeyPin.Parse(HostKey.Replace("ssh-ed25519", "ssh-rsa"), Fingerprint));
    }

    [TestMethod]
    public void ProfileRoundTripIsClosedAndContainsOnlyProtectedReference()
    {
        var path = Path.Combine(Path.GetTempPath(), "wakepilot-profile-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var profile = LabProfile.Create(
                BridgeId.New(),
                TargetId.New(),
                "100.64.0.10",
                8023,
                "u0_a123",
                new SecretReference("dpapi:" + Guid.NewGuid().ToString("D")));

            profile.Save(path);
            var loaded = LabProfile.Load(path);

            Assert.AreEqual(profile, loaded);
            var json = File.ReadAllText(path);
            StringAssert.Contains(json, "dpapi:");
            Assert.IsFalse(json.Contains("OPENSSH PRIVATE KEY", StringComparison.Ordinal));

            File.WriteAllText(path, json.TrimEnd().TrimEnd('}') + ",\"unexpected\":true}");
            Assert.Throws<InvalidDataException>(() => LabProfile.Load(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void CommandLineRejectsUnknownAndRepeatedOptions()
    {
        Assert.Throws<ArgumentException>(() => CommandLine.Parse(["wake", "--host", "example"]));
        Assert.Throws<ArgumentException>(() => CommandLine.Parse([
            "configure", "--host", "a", "--host", "b"]));
        Assert.Throws<ArgumentException>(() => CommandLine.Parse(["shell"]));
    }
}
