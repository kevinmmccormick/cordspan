using Cordspan.Services;

namespace Cordspan.Tests.Fakes;

internal sealed class FakeNetworkProbe : INetworkProbe
{
    private readonly HashSet<string> reachableHosts = new(StringComparer.OrdinalIgnoreCase);

    public void AddReachableHost(string host)
    {
        reachableHosts.Add(host);
    }

    public Task<bool> IsTcpPortOpenAsync(string host, int port, TimeSpan timeout, CancellationToken cancellationToken)
    {
        return Task.FromResult(reachableHosts.Contains(host));
    }
}
