using RemoteWake.Application.Models;
using RemoteWake.Infrastructure.Processes;

namespace RemoteWake.M1.ContractTests;

[TestClass]
public sealed class ProcessBoundaryTests
{
    [TestMethod]
    public void Ct014ProcessBoundaryNeverUsesShellAndPreservesArgumentBoundaries()
    {
        var executable = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "safe-tool.exe"));
        var hostileArgument = "value & whoami; $(id) `calc`";
        var invocation = new ProcessInvocation(
            executable,
            ["--value", hostileArgument],
            TimeSpan.FromSeconds(3));

        var startInfo = SystemProcessRunner.CreateStartInfo(invocation);

        Assert.IsFalse(startInfo.UseShellExecute);
        Assert.IsTrue(startInfo.RedirectStandardOutput);
        Assert.IsTrue(startInfo.RedirectStandardError);
        Assert.IsTrue(startInfo.RedirectStandardInput);
        CollectionAssert.AreEqual(new[] { "--value", hostileArgument }, startInfo.ArgumentList.ToArray());
        Assert.AreEqual(string.Empty, startInfo.Arguments);
    }

    [TestMethod]
    public void Ct014ProcessBoundaryRejectsRelativeOrNonCanonicalExecutable()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new ProcessInvocation("tool.exe", [], TimeSpan.FromSeconds(1)));

        var nonCanonical = Path.Combine(Path.GetTempPath(), "folder", "..", "tool.exe");
        Assert.ThrowsExactly<ArgumentException>(() =>
            new ProcessInvocation(nonCanonical, [], TimeSpan.FromSeconds(1)));
    }
}
