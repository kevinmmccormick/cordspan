using System.Text.RegularExpressions;
using Cordspan.Models;

namespace Cordspan.Services;

public static partial class UsbipWin2Parser
{
    public static IReadOnlyList<RemoteUsbDevice> ParseRemoteList(string host, string output)
    {
        var devices = new List<RemoteUsbDevice>();

        foreach (var rawLine in output.Split(["\r\n", "\n"], StringSplitOptions.None))
        {
            var line = rawLine.TrimEnd();
            var deviceMatch = RemoteDeviceLine().Match(line);
            if (!deviceMatch.Success)
            {
                continue;
            }

            devices.Add(new RemoteUsbDevice(
                host,
                deviceMatch.Groups["busid"].Value,
                deviceMatch.Groups["vid"].Value.ToUpperInvariant(),
                deviceMatch.Groups["pid"].Value.ToUpperInvariant(),
                deviceMatch.Groups["name"].Value.Trim()));
        }

        return devices;
    }

    public static IReadOnlyList<ImportedUsbDevice> ParsePorts(string output)
    {
        var devices = new List<ImportedUsbDevice>();
        int? port = null;
        string? host = null;
        string? busId = null;
        string? vid = null;
        string? pid = null;
        string? name = null;

        foreach (var rawLine in output.Split(["\r\n", "\n"], StringSplitOptions.None))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var portMatch = PortLine().Match(line);
            if (portMatch.Success)
            {
                port = int.Parse(portMatch.Groups["port"].Value);
                host = null;
                busId = null;
                vid = null;
                pid = null;
                name = null;
                continue;
            }

            var remoteMatch = RemoteImportLine().Match(line);
            if (remoteMatch.Success)
            {
                host = FirstGroupValue(remoteMatch, "host", "host2");
                busId = FirstGroupValue(remoteMatch, "busid", "busid2");
                if (port.HasValue && name is not null && vid is not null && pid is not null)
                {
                    devices.Add(new ImportedUsbDevice(
                        port.Value,
                        host,
                        busId,
                        vid,
                        pid,
                        name));
                }

                continue;
            }

            var deviceMatch = PortDeviceLine().Match(line);
            if (deviceMatch.Success)
            {
                vid = FirstGroupValue(deviceMatch, "vid", "vid2").ToUpperInvariant();
                pid = FirstGroupValue(deviceMatch, "pid", "pid2").ToUpperInvariant();
                name = FirstGroupValue(deviceMatch, "name", "name2").Trim();
            }
        }

        return devices;
    }

    private static string FirstGroupValue(Match match, string primary, string alternate)
    {
        return match.Groups[primary].Success
            ? match.Groups[primary].Value
            : match.Groups[alternate].Value;
    }

    [GeneratedRegex(@"^\s*(?:-\s+(?:busid\s+)?)?(?<busid>\d+-\d+):?\s+(?<name>.+?)\s+\((?<vid>[0-9A-Fa-f]{4}):(?<pid>[0-9A-Fa-f]{4})\)", RegexOptions.Compiled)]
    private static partial Regex RemoteDeviceLine();

    [GeneratedRegex(@"^Port\s+(?<port>\d+):", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex PortLine();

    [GeneratedRegex(@"^(?:Remote\s+host:\s+(?<host>.+?),\s+Remote\s+port:\s+\d+,\s+Busid\s+(?<busid>\S+)|\S+\s+->\s+usbip://(?<host2>[^/:]+):\d+/(?<busid2>\S+))", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex RemoteImportLine();

    [GeneratedRegex(@"^(?:(?<vid>[0-9A-Fa-f]{4}):(?<pid>[0-9A-Fa-f]{4})\s+:\s+(?<name>.+)|(?<name2>.+?)\s+\((?<vid2>[0-9A-Fa-f]{4}):(?<pid2>[0-9A-Fa-f]{4})\))$", RegexOptions.Compiled)]
    private static partial Regex PortDeviceLine();
}
