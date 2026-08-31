namespace Cordspan.Services;

[Obsolete("Use UsbipdWinService for host-side sharing and UsbipWin2Service for client-side attachment.")]
public sealed class UsbipdService
{
    private readonly UsbipdWinService hostService = new();

    public string ExecutablePath => hostService.ExecutablePath;

    public Task<IReadOnlyList<Models.UsbDevice>> ListDevicesAsync(CancellationToken cancellationToken)
        => hostService.ListLocalDevicesAsync(cancellationToken);

    public Task<CommandResult> BindAsync(string busId, CancellationToken cancellationToken)
        => hostService.ShareAsync(busId, force: false, cancellationToken);

    public Task<CommandResult> UnbindAsync(string busId, CancellationToken cancellationToken)
        => hostService.StopSharingAsync(busId, cancellationToken);
}
