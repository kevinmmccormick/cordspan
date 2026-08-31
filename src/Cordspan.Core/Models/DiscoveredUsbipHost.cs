namespace Cordspan.Models;

public sealed class DiscoveredUsbipHost
{
    public DiscoveredUsbipHost(string host, int port, bool isReachable, int exportedDeviceCount, string status)
    {
        Host = host;
        Port = port;
        IsReachable = isReachable;
        ExportedDeviceCount = exportedDeviceCount;
        Status = status;
    }

    public string Host { get; set; }

    public int Port { get; set; }

    public bool IsReachable { get; set; }

    public int ExportedDeviceCount { get; set; }

    public string Status { get; set; }
}
