using Cordspan.Services;

namespace Cordspan.Tests.Fakes;

internal sealed class FakeCommandRunner : ICommandRunner
{
    private readonly Queue<CommandResult> results = [];

    public List<(string ExecutablePath, IReadOnlyList<string> Arguments)> Calls { get; } = [];

    public void Enqueue(CommandResult result)
    {
        results.Enqueue(result);
    }

    public Task<CommandResult> RunAsync(string executablePath, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        Calls.Add((executablePath, arguments.ToArray()));

        if (results.Count == 0)
        {
            throw new InvalidOperationException("No fake command result was queued.");
        }

        return Task.FromResult(results.Dequeue());
    }
}
