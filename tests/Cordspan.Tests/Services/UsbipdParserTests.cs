using Cordspan.Services;

namespace Cordspan.Tests.Services;

[TestClass]
public sealed class UsbipdParserTests
{
    [TestMethod]
    public void ParseList_ReturnsConnectedDevicesAndIgnoresPersistedSection()
    {
        const string output = """
            Connected:
            BUSID  VID:PID    DEVICE                                                        STATE
            1-7    045e:02e6  Xbox Wireless Adapter for Windows                              Not shared
            2-3    28de:1205  Valve Software Steam Controller Receiver                       Shared
            4-1    046d:c262  Logitech G920 Driving Force Racing Wheel                       Attached

            Persisted:
            GUID                                  DEVICE
            {01bb8f81-2ad5-45bb-b009-0615d5ecb31a}  Some old device
            """;

        var devices = UsbipdParser.ParseList(output);

        Assert.HasCount(3, devices);
        Assert.AreEqual("1-7", devices[0].BusId);
        Assert.AreEqual("045E", devices[0].Vid);
        Assert.AreEqual("02E6", devices[0].Pid);
        Assert.AreEqual("Xbox Wireless Adapter for Windows", devices[0].Name);
        Assert.AreEqual("Not shared", devices[0].State);
        Assert.IsTrue(devices[1].IsShared);
        Assert.IsTrue(devices[2].IsAttached);
    }
}
