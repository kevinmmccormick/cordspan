using Cordspan.Models;

namespace Cordspan.Services;

public sealed class UsbipWin2Service
{
    private readonly ICommandRunner commandRunner;
    private readonly ExecutableResolution executable;

    public UsbipWin2Service()
        : this(new ProcessCommandRunner(), ExecutableResolver.ResolveDetailed("usbip.exe", "usbip-win2"))
    {
    }

    public UsbipWin2Service(ICommandRunner commandRunner, string executablePath)
        : this(commandRunner, new ExecutableResolution(executablePath, true, [executablePath]))
    {
    }

    private UsbipWin2Service(ICommandRunner commandRunner, ExecutableResolution executable)
    {
        this.commandRunner = commandRunner;
        this.executable = executable;
    }

    public string ExecutablePath => executable.ExecutablePath;

    public async Task<IReadOnlyList<RemoteUsbDevice>> ListRemoteDevicesAsync(string host, CancellationToken cancellationToken)
    {
        EnsureExecutableAvailable();
        var result = await commandRunner.RunAsync(executable.ExecutablePath, ["list", "--remote", host], cancellationToken);
        EnsureSuccess(result, $"Unable to list USB devices on {host}.");
        return UsbipWin2Parser.ParseRemoteList(host, result.StandardOutput);
    }

    public async Task<CommandResult> AttachAsync(string host, string busId, CancellationToken cancellationToken)
    {
        EnsureExecutableAvailable();
        var result = await commandRunner.RunAsync(executable.ExecutablePath, ["attach", "--remote", host, "--busid", busId], cancellationToken);
        EnsureSuccess(result, $"Unable to attach {busId} from {host}.");
        return result;
    }

    public async Task<IReadOnlyList<ImportedUsbDevice>> ListImportedPortsAsync(CancellationToken cancellationToken)
    {
        EnsureExecutableAvailable();
        var result = await commandRunner.RunAsync(executable.ExecutablePath, ["port"], cancellationToken);
        EnsureSuccess(result, "Unable to list imported USB devices.");
        return UsbipWin2Parser.ParsePorts(result.StandardOutput);
    }

    public async Task<CommandResult> DetachAsync(int port, CancellationToken cancellationToken)
    {
        EnsureExecutableAvailable();
        var result = await commandRunner.RunAsync(executable.ExecutablePath, ["detach", "--port", port.ToString()], cancellationToken);
        EnsureSuccess(result, $"Unable to detach imported USB port {port}.");
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

    private void EnsureExecutableAvailable()
    {
        if (!executable.IsAvailable)
        {
            throw new UsbipdException(executable.MissingExecutableMessage);
        }
    }
}
