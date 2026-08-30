using Cordspan.Models;

namespace Cordspan.Services;

public sealed class NetworkDiscoveryService
{
    public const int DefaultUsbipPort = 3240;

    private readonly INetworkProbe networkProbe;
    private readonly UsbipWin2Service clientService;

    public NetworkDiscoveryService()
        : this(new TcpNetworkProbe(), new UsbipWin2Service())
    {
    }

    public NetworkDiscoveryService(INetworkProbe networkProbe, UsbipWin2Service clientService)
    {
        this.networkProbe = networkProbe;
        this.clientService = clientService;
    }

    public async Task<IReadOnlyList<DiscoveredUsbipHost>> ValidateHostsAsync(
        IEnumerable<string> hosts,
        int port = DefaultUsbipPort,
        CancellationToken cancellationToken = default)
    {
        var results = new List<DiscoveredUsbipHost>();

        foreach (var host in NormalizeHosts(hosts))
        {
            var reachable = await networkProbe.IsTcpPortOpenAsync(host, port, TimeSpan.FromMilliseconds(650), cancellationToken);
            if (!reachable)
            {
                results.Add(new DiscoveredUsbipHost(host, port, isReachable: false, exportedDeviceCount: 0, "Port closed or unreachable"));
                continue;
            }

            try
            {
                var devices = await clientService.ListRemoteDevicesAsync(host, cancellationToken);
                results.Add(new DiscoveredUsbipHost(host, port, isReachable: true, devices.Count, "USB/IP host"));
            }
            catch (UsbipdException ex)
            {
                results.Add(new DiscoveredUsbipHost(host, port, isReachable: true, exportedDeviceCount: 0, $"Port open, query failed: {ex.Message}"));
            }
        }

        return results;
    }

    public static IReadOnlyList<string> CreateClassCSubnetCandidates(string ipv4Address)
    {
        var parts = ipv4Address.Split('.');
        if (parts.Length != 4
            || !parts.All(part => byte.TryParse(part, out _))
            || !byte.TryParse(parts[3], out var currentHostOctet))
        {
            throw new ArgumentException("Expected an IPv4 address such as 192.168.1.25.", nameof(ipv4Address));
        }

        var prefix = $"{parts[0]}.{parts[1]}.{parts[2]}";
        return Enumerable.Range(1, 254)
            .Where(octet => octet != currentHostOctet)
            .Select(octet => $"{prefix}.{octet}")
            .ToArray();
    }

    private static IReadOnlyList<string> NormalizeHosts(IEnumerable<string> hosts)
    {
        return hosts
            .Select(host => host.Trim())
            .Where(host => !string.IsNullOrWhiteSpace(host))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
