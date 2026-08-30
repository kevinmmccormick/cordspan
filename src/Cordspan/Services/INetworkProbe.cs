namespace Cordspan.Services;

public interface INetworkProbe
{
    Task<bool> IsTcpPortOpenAsync(string host, int port, TimeSpan timeout, CancellationToken cancellationToken);
}
