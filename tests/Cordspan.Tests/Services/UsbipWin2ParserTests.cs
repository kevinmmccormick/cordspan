using Cordspan.Services;

namespace Cordspan.Tests.Services;

[TestClass]
public sealed class UsbipWin2ParserTests
{
    [TestMethod]
    public void ParseRemoteList_ReturnsExportedDevices()
    {
        const string output = """
            Exportable USB devices
            ======================
             - 192.168.1.12
                  2-3: Valve Software : Steam Controller Receiver (28de:1205)
                     : /sys/devices/pci0000:00/0000:00:14.0/usb2/2-3
                     : (Defined at Interface level) (00/00/00)

                  3-2: Microsoft Corp. : Xbox Wireless Adapter (045e:02e6)
                     : /sys/devices/pci0000:00/0000:00:14.0/usb3/3-2
                     : (Defined at Interface level) (00/00/00)
            """;

        var devices = UsbipWin2Parser.ParseRemoteList("192.168.1.12", output);

        Assert.HasCount(2, devices);
        Assert.AreEqual("192.168.1.12", devices[0].Host);
        Assert.AreEqual("2-3", devices[0].BusId);
        Assert.AreEqual("28DE", devices[0].Vid);
        Assert.AreEqual("1205", devices[0].Pid);
        Assert.AreEqual("Valve Software : Steam Controller Receiver", devices[0].Name);
    }

    [TestMethod]
    public void ParsePorts_ReturnsImportedDevices()
    {
        const string output = """
            Imported USB devices
            ====================
            Port 00: <Port in Use> at High Speed(480Mbps)
                   Valve Software : Steam Controller Receiver (28de:1205)
                   6-1 -> usbip://192.168.1.12:3240/2-3
                   -> remote bus/dev 002/003

            Port 01: <Port in Use> at Full Speed(12Mbps)
                   Microsoft Corp. : Xbox Wireless Adapter (045e:02e6)
                   7-1 -> usbip://gamepc.local:3240/3-2
                   -> remote bus/dev 003/002
            """;

        var ports = UsbipWin2Parser.ParsePorts(output);

        Assert.HasCount(2, ports);
        Assert.AreEqual(0, ports[0].Port);
        Assert.AreEqual("192.168.1.12", ports[0].RemoteHost);
        Assert.AreEqual("2-3", ports[0].RemoteBusId);
        Assert.AreEqual("28DE", ports[0].Vid);
        Assert.AreEqual("1205", ports[0].Pid);
        Assert.AreEqual("Valve Software : Steam Controller Receiver", ports[0].Name);
    }
}
