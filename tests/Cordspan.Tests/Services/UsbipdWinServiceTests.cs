using Cordspan.Services;
using Cordspan.Tests.Fakes;

namespace Cordspan.Tests.Services;

[TestClass]
public sealed class UsbipdWinServiceTests
{
    [TestMethod]
    public async Task ShareAsync_UsesBindWithoutForceByDefault()
    {
        var runner = new FakeCommandRunner();
        runner.Enqueue(new CommandResult(0, string.Empty, string.Empty));
        var service = new UsbipdWinService(runner, "usbipd.exe");

        await service.ShareAsync("2-3", force: false, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "bind", "--busid", "2-3" }, runner.Calls.Single().Arguments.ToArray());
    }

    [TestMethod]
    public async Task ShareAsync_UsesForceWhenRequested()
    {
        var runner = new FakeCommandRunner();
        runner.Enqueue(new CommandResult(0, string.Empty, string.Empty));
        var service = new UsbipdWinService(runner, "usbipd.exe");

        await service.ShareAsync("2-3", force: true, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "bind", "--force", "--busid", "2-3" }, runner.Calls.Single().Arguments.ToArray());
    }

    [TestMethod]
    public async Task StopSharingAsync_UsesUnbind()
    {
        var runner = new FakeCommandRunner();
        runner.Enqueue(new CommandResult(0, string.Empty, string.Empty));
        var service = new UsbipdWinService(runner, "usbipd.exe");

        await service.StopSharingAsync("2-3", CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "unbind", "--busid", "2-3" }, runner.Calls.Single().Arguments.ToArray());
    }

    [TestMethod]
    public async Task ListLocalDevicesAsync_ThrowsUsefulFailure()
    {
        var runner = new FakeCommandRunner();
        runner.Enqueue(new CommandResult(1, string.Empty, "access denied"));
        var service = new UsbipdWinService(runner, "usbipd.exe");

        var ex = await Assert.ThrowsExactlyAsync<UsbipdException>(() => service.ListLocalDevicesAsync(CancellationToken.None));

        StringAssert.Contains(ex.Message, "Unable to list local USB devices.");
        StringAssert.Contains(ex.Message, "access denied");
    }
}
