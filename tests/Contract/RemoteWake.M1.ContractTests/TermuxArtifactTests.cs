namespace RemoteWake.M1.ContractTests;

[TestClass]
public sealed class TermuxArtifactTests
{
    [TestMethod]
    public void Ct010AndCt011BootstrapRequiresConsentAndForcedCommandHasAllRestrictions()
    {
        var root = FindRepositoryRoot();
        var bootstrap = File.ReadAllText(Path.Combine(root, "android", "termux-bootstrap", "bootstrap.sh"));
        var installer = File.ReadAllText(Path.Combine(root, "android", "termux-bootstrap", "install_key.py"));
        var targetConfigurator = File.ReadAllText(
            Path.Combine(root, "android", "termux-bootstrap", "configure_target.py"));
        var wrapper = File.ReadAllText(Path.Combine(root, "android", "termux-bootstrap", "rwa_bridge.py"));

        StringAssert.Contains(bootstrap, "--apply");
        StringAssert.Contains(bootstrap, "--sshd-port");
        StringAssert.Contains(bootstrap, "--target-id");
        StringAssert.Contains(bootstrap, "command -v sshd");
        Assert.IsFalse(bootstrap.Contains("pkg install -y python openssh", StringComparison.Ordinal));
        StringAssert.Contains(bootstrap, "PasswordAuthentication no");
        StringAssert.Contains(bootstrap, "AllowTcpForwarding no");
        StringAssert.Contains(bootstrap, "termux-wake-lock >/dev/null 2>&1 || true");
        StringAssert.Contains(bootstrap, "remote-wake-$KEY_ID$|d");
        StringAssert.Contains(installer, "restrict,command=");
        StringAssert.Contains(installer, "no-agent-forwarding");
        StringAssert.Contains(installer, "no-port-forwarding");
        StringAssert.Contains(installer, "no-X11-forwarding");
        StringAssert.Contains(installer, "no-pty");
        StringAssert.Contains(installer, "no-user-rc");
        StringAssert.Contains(targetConfigurator, "load_targets(config_path)");
        Assert.IsFalse(targetConfigurator.Contains("shell=True", StringComparison.Ordinal));
        Assert.IsFalse(wrapper.Contains("eval(", StringComparison.Ordinal));
        Assert.IsFalse(wrapper.Contains("subprocess", StringComparison.Ordinal));
        Assert.IsFalse(wrapper.Contains("os.system", StringComparison.Ordinal));
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "RemoteWake.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
