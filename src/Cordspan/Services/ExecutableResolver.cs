namespace Cordspan.Services;

public static class ExecutableResolver
{
    public static string Resolve(string executableName, string? repoFolder = null)
        => ResolveDetailed(executableName, repoFolder).ExecutablePath;

    public static ExecutableResolution ResolveDetailed(
        string executableName,
        string? repoFolder = null,
        string? baseDirectory = null,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        baseDirectory ??= AppContext.BaseDirectory;
        environment ??= ReadEnvironment();

        var candidates = new List<string>
        {
            Path.Combine(baseDirectory, executableName)
        };

        if (!string.IsNullOrWhiteSpace(repoFolder))
        {
            candidates.Add(Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", repoFolder, executableName)));
        }

        AddPathCandidates(candidates, executableName, environment);

        if (string.Equals(repoFolder, "usbip-win2", StringComparison.OrdinalIgnoreCase))
        {
            AddInstallCandidate(candidates, environment, "ProgramFiles", "USBip", executableName);
            AddInstallCandidate(candidates, environment, "ProgramFiles", "usbip-win2", executableName);
            AddInstallCandidate(candidates, environment, "ProgramFiles(x86)", "USBip", executableName);
            AddInstallCandidate(candidates, environment, "ProgramFiles(x86)", "usbip-win2", executableName);
        }

        var checkedPaths = candidates
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var candidate in checkedPaths)
        {
            if (File.Exists(candidate))
            {
                return new ExecutableResolution(candidate, true, checkedPaths);
            }
        }

        return new ExecutableResolution(executableName, false, checkedPaths);
    }

    private static Dictionary<string, string?> ReadEnvironment()
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Path"] = Environment.GetEnvironmentVariable("Path"),
            ["UserPath"] = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User),
            ["MachinePath"] = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine),
            ["ProgramFiles"] = Environment.GetEnvironmentVariable("ProgramFiles"),
            ["ProgramFiles(x86)"] = Environment.GetEnvironmentVariable("ProgramFiles(x86)")
        };
    }

    private static void AddPathCandidates(
        ICollection<string> candidates,
        string executableName,
        IReadOnlyDictionary<string, string?> environment)
    {
        foreach (var key in new[] { "Path", "UserPath", "MachinePath" })
        {
            if (!environment.TryGetValue(key, out var pathValue) || string.IsNullOrWhiteSpace(pathValue))
            {
                continue;
            }

            foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                candidates.Add(Path.Combine(Environment.ExpandEnvironmentVariables(directory.Trim('"')), executableName));
            }
        }
    }

    private static void AddInstallCandidate(
        ICollection<string> candidates,
        IReadOnlyDictionary<string, string?> environment,
        string environmentKey,
        string productFolder,
        string executableName)
    {
        if (environment.TryGetValue(environmentKey, out var root) && !string.IsNullOrWhiteSpace(root))
        {
            candidates.Add(Path.Combine(root, productFolder, executableName));
        }
    }
}

public sealed record ExecutableResolution(
    string ExecutablePath,
    bool IsAvailable,
    IReadOnlyList<string> CheckedPaths)
{
    public string MissingExecutableMessage =>
        $"{ExecutablePath} was not found. Checked: {string.Join("; ", CheckedPaths)}. " +
        "Install usbip-win2, copy usbip.exe next to Cordspan, or add its folder to PATH and restart Cordspan.";
}
