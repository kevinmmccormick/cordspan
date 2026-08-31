namespace Cordspan.Models;

public sealed class ImportedUsbDevice
{
    public ImportedUsbDevice(int port, string remoteHost, string remoteBusId, string vid, string pid, string name)
    {
        Port = port;
        RemoteHost = remoteHost;
        RemoteBusId = remoteBusId;
        Vid = vid;
        Pid = pid;
        Name = name;
    }

    public int Port { get; set; }

    public string RemoteHost { get; set; }

    public string RemoteBusId { get; set; }

    public string Vid { get; set; }

    public string Pid { get; set; }

    public string Name { get; set; }

    public string VidPid => $"{Vid}:{Pid}";
}
