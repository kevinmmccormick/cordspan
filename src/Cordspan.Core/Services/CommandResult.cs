namespace Cordspan.Services;

public sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;

    public string DisplayText => string.IsNullOrWhiteSpace(StandardError)
        ? StandardOutput.Trim()
        : StandardError.Trim();
}
