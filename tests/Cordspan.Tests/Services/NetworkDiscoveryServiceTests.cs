using Cordspan.Services;
using Cordspan.Tests.Fakes;

namespace Cordspan.Tests.Services;

[TestClass]
public sealed class NetworkDiscoveryServiceTests
{
    [TestMethod]
    public async Task ValidateHostsAsync_MarksClosedHostWithoutRunningUsbip()
    {
        var probe = new FakeNetworkProbe();
        var runner = new FakeCommandRunner();
        var client = new UsbipWin2Service(runner, "usbip.exe");
        var discovery = new NetworkDiscoveryService(probe, client);

        var results = await discovery.ValidateHostsAsync(["gamepc.local"], cancellationToken: CancellationToken.None);

        Assert.HasCount(1, results);
        Assert.IsFalse(results[0].IsReachable);
        Assert.IsEmpty(runner.Calls);
    }

    [TestMethod]
    public async Task ValidateHostsAsync_QueriesOpenHostAndCountsExports()
    {
        var probe = new FakeNetworkProbe();
        probe.AddReachableHost("gamepc.local");

        var runner = new FakeCommandRunner();
        runner.Enqueue(new CommandResult(0, """
             - 2-3: Valve Software : Steam Controller Receiver (28de:1205)
            """, string.Empty));

        var client = new UsbipWin2Service(runner, "usbip.exe");
        var discovery = new NetworkDiscoveryService(probe, client);

        var results = await discovery.ValidateHostsAsync(["gamepc.local"], cancellationToken: CancellationToken.None);

        Assert.HasCount(1, results);
        Assert.IsTrue(results[0].IsReachable);
        Assert.AreEqual(1, results[0].ExportedDeviceCount);
        CollectionAssert.AreEqual(new[] { "list", "--remote", "gamepc.local" }, runner.Calls.Single().Arguments.ToArray());
    }

    [TestMethod]
    public void CreateClassCSubnetCandidates_ReturnsOtherHostsInSubnet()
    {
        var candidates = NetworkDiscoveryService.CreateClassCSubnetCandidates("192.168.50.42");

        Assert.HasCount(253, candidates);
        CollectionAssert.Contains(candidates.ToList(), "192.168.50.1");
        CollectionAssert.Contains(candidates.ToList(), "192.168.50.254");
        CollectionAssert.DoesNotContain(candidates.ToList(), "192.168.50.42");
    }
}
