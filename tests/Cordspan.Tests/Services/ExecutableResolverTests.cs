using Cordspan.Services;

namespace Cordspan.Tests.Services;

[TestClass]
public sealed class ExecutableResolverTests
{
    [TestMethod]
    public void ResolveDetailed_FindsExecutableOnMachinePath()
    {
        using var fixture = new ResolverFixture();
        var pathDirectory = fixture.CreateDirectory("machine-path");
        var executable = fixture.CreateExecutable(pathDirectory);

        var result = ExecutableResolver.ResolveDetailed(
            "usbip.exe",
            "usbip-win2",
            fixture.BaseDirectory,
            fixture.Environment(machinePath: pathDirectory));

        Assert.IsTrue(result.IsAvailable);
        Assert.AreEqual(executable, result.ExecutablePath);
    }

    [TestMethod]
    public void ResolveDetailed_FindsUsbipWin2InstallerLocation()
    {
        using var fixture = new ResolverFixture();
        var executable = fixture.CreateExecutable(Path.Combine(fixture.Root, "Program Files", "USBip"));

        var result = ExecutableResolver.ResolveDetailed(
            "usbip.exe",
            "usbip-win2",
            fixture.BaseDirectory,
            fixture.Environment(programFiles: Path.Combine(fixture.Root, "Program Files")));

        Assert.IsTrue(result.IsAvailable);
        Assert.AreEqual(executable, result.ExecutablePath);
    }

    [TestMethod]
    public void ResolveDetailed_WhenMissing_ReportsCheckedLocations()
    {
        using var fixture = new ResolverFixture();
        var programFiles = Path.Combine(fixture.Root, "Program Files");

        var result = ExecutableResolver.ResolveDetailed(
            "usbip.exe",
            "usbip-win2",
            fixture.BaseDirectory,
            fixture.Environment(programFiles: programFiles));

        Assert.IsFalse(result.IsAvailable);
        StringAssert.Contains(result.MissingExecutableMessage, Path.Combine(programFiles, "USBip", "usbip.exe"));
        StringAssert.Contains(result.MissingExecutableMessage, "add its folder to PATH");
    }

    private sealed class ResolverFixture : IDisposable
    {
        public ResolverFixture()
        {
            Root = Path.Combine(Path.GetTempPath(), $"Cordspan-{Guid.NewGuid():N}");
            BaseDirectory = CreateDirectory("app");
        }

        public string Root { get; }
        public string BaseDirectory { get; }

        public string CreateDirectory(string relativePath)
        {
            var path = Path.Combine(Root, relativePath);
            Directory.CreateDirectory(path);
            return path;
        }

        public string CreateExecutable(string directory)
        {
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "usbip.exe");
            File.WriteAllText(path, string.Empty);
            return path;
        }

        public IReadOnlyDictionary<string, string?> Environment(
            string? machinePath = null,
            string? programFiles = null)
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Path"] = null,
                ["UserPath"] = null,
                ["MachinePath"] = machinePath,
                ["ProgramFiles"] = programFiles,
                ["ProgramFiles(x86)"] = null
            };
        }

        public void Dispose()
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}
