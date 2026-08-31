namespace Cordspan.Models;

public sealed class RemoteUsbDevice
{
    public RemoteUsbDevice(string host, string busId, string vid, string pid, string name)
    {
        Host = host;
        BusId = busId;
        Vid = vid;
        Pid = pid;
        Name = name;
    }

    public string Host { get; set; }

    public string BusId { get; set; }

    public string Vid { get; set; }

    public string Pid { get; set; }

    public string Name { get; set; }

    public string VidPid => $"{Vid}:{Pid}";
}
