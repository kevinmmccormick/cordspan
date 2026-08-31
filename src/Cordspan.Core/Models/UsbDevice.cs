namespace Cordspan.Models;

public sealed record UsbDevice(
    string BusId,
    string Vid,
    string Pid,
    string Name,
    string State)
{
    public string VidPid => $"{Vid}:{Pid}";

    public bool IsShared => State.Contains("Shared", StringComparison.OrdinalIgnoreCase);

    public bool IsAttached => State.Contains("Attached", StringComparison.OrdinalIgnoreCase);

    public bool IsAvailable => !IsAttached && !State.Contains("Unavailable", StringComparison.OrdinalIgnoreCase);
}
