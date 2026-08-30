using Cordspan.Services;
using Cordspan.Tests.Fakes;

namespace Cordspan.Tests.Services;

[TestClass]
public sealed class UsbipWin2ServiceTests
{
    [TestMethod]
    public async Task ListRemoteDevicesAsync_UsesUsbipRemoteList()
    {
        var runner = new FakeCommandRunner();
        runner.Enqueue(new CommandResult(0, string.Empty, string.Empty));
        var service = new UsbipWin2Service(runner, "usbip.exe");

        await service.ListRemoteDevicesAsync("gamepc.local", CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "list", "--remote", "gamepc.local" }, runner.Calls.Single().Arguments.ToArray());
    }

    [TestMethod]
    public async Task AttachAsync_UsesRemoteAndBusId()
    {
        var runner = new FakeCommandRunner();
        runner.Enqueue(new CommandResult(0, string.Empty, string.Empty));
        var service = new UsbipWin2Service(runner, "usbip.exe");

        await service.AttachAsync("gamepc.local", "2-3", CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "attach", "--remote", "gamepc.local", "--busid", "2-3" }, runner.Calls.Single().Arguments.ToArray());
    }

    [TestMethod]
    public async Task ListImportedPortsAsync_UsesPortCommand()
    {
        var runner = new FakeCommandRunner();
        runner.Enqueue(new CommandResult(0, string.Empty, string.Empty));
        var service = new UsbipWin2Service(runner, "usbip.exe");

        await service.ListImportedPortsAsync(CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "port" }, runner.Calls.Single().Arguments.ToArray());
    }

    [TestMethod]
    public async Task DetachAsync_UsesPortOption()
    {
        var runner = new FakeCommandRunner();
        runner.Enqueue(new CommandResult(0, string.Empty, string.Empty));
        var service = new UsbipWin2Service(runner, "usbip.exe");

        await service.DetachAsync(3, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "detach", "--port", "3" }, runner.Calls.Single().Arguments.ToArray());
    }
}
