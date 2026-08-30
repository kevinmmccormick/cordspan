using Cordspan.Models;

namespace Cordspan.Services;

public sealed class UsbipdWinService
{
    private readonly ICommandRunner commandRunner;
    private readonly string executablePath;

    public UsbipdWinService()
        : this(new ProcessCommandRunner(), ExecutableResolver.Resolve("usbipd.exe", "usbipd-win"))
    {
    }

    public UsbipdWinService(ICommandRunner commandRunner, string executablePath)
    {
        this.commandRunner = commandRunner;
        this.executablePath = executablePath;
    }

    public string ExecutablePath => executablePath;

    public async Task<IReadOnlyList<UsbDevice>> ListLocalDevicesAsync(CancellationToken cancellationToken)
    {
        var result = await commandRunner.RunAsync(executablePath, ["list"], cancellationToken);
        EnsureSuccess(result, "Unable to list local USB devices.");
        return UsbipdParser.ParseList(result.StandardOutput);
    }

    public async Task<CommandResult> ShareAsync(string busId, bool force, CancellationToken cancellationToken)
    {
        var args = force
            ? new[] { "bind", "--force", "--busid", busId }
            : ["bind", "--busid", busId];

        var result = await commandRunner.RunAsync(executablePath, args, cancellationToken);
        EnsureSuccess(result, $"Unable to share device {busId}.");
        return result;
    }

    public async Task<CommandResult> StopSharingAsync(string busId, CancellationToken cancellationToken)
    {
        var result = await commandRunner.RunAsync(executablePath, ["unbind", "--busid", busId], cancellationToken);
        EnsureSuccess(result, $"Unable to stop sharing device {busId}.");
        return result;
    }

    private static void EnsureSuccess(CommandResult result, string fallbackMessage)
    {
        if (result.Succeeded)
        {
            return;
        }

        var detail = result.DisplayText;
        throw new UsbipdException(string.IsNullOrWhiteSpace(detail) ? fallbackMessage : $"{fallbackMessage} {detail}");
    }
}
