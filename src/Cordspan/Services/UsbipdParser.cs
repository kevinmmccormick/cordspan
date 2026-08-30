using System.Text.RegularExpressions;
using Cordspan.Models;

namespace Cordspan.Services;

public static partial class UsbipdParser
{
    public static IReadOnlyList<UsbDevice> ParseList(string output)
    {
        var devices = new List<UsbDevice>();
        var inConnectedSection = false;

        foreach (var rawLine in output.Split(["\r\n", "\n"], StringSplitOptions.None))
        {
            var line = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("Connected:", StringComparison.OrdinalIgnoreCase))
            {
                inConnectedSection = true;
                continue;
            }

            if (line.StartsWith("Persisted:", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (!inConnectedSection || line.StartsWith("BUSID", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var match = DeviceLine().Match(line.Trim());
            if (!match.Success)
            {
                continue;
            }

            var vidPid = match.Groups["vidpid"].Value.Split(':');
            devices.Add(new UsbDevice(
                match.Groups["busid"].Value,
                vidPid[0].ToUpperInvariant(),
                vidPid[1].ToUpperInvariant(),
                match.Groups["device"].Value.Trim(),
                match.Groups["state"].Value.Trim()));
        }

        return devices;
    }

    [GeneratedRegex(@"^(?<busid>\S+)\s+(?<vidpid>[0-9A-Fa-f]{4}:[0-9A-Fa-f]{4})\s+(?<device>.+?)\s{2,}(?<state>.+)$", RegexOptions.Compiled)]
    private static partial Regex DeviceLine();
}
