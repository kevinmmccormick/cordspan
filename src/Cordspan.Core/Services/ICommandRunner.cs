namespace Cordspan.Services;

public interface ICommandRunner
{
    Task<CommandResult> RunAsync(string executablePath, IReadOnlyList<string> arguments, CancellationToken cancellationToken);
}
