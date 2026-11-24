namespace NugetScope.Core.Solutions;

/// <summary>Finds solution files on disk.</summary>
public static class SolutionLocator
{
    private static readonly string[] Patterns = ["*.sln", "*.slnx"];

    /// <summary>
    /// Resolves an optional path argument: a solution file is used as-is, a directory is searched,
    /// and no argument searches <paramref name="currentDirectory"/>.
    /// </summary>
    public static SolutionResolution Resolve(string? argument, string currentDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);

        string directory = currentDirectory;

        if (!string.IsNullOrWhiteSpace(argument))
        {
            string fullPath = Path.GetFullPath(argument, currentDirectory);

            if (File.Exists(fullPath))
            {
                return IsSolutionFile(fullPath)
                    ? new SolutionResolution.Found(fullPath)
                    : new SolutionResolution.NotFound($"Not a solution file (expected .sln or .slnx): {fullPath}");
            }

            if (!Directory.Exists(fullPath))
            {
                return new SolutionResolution.NotFound($"Path not found: {fullPath}");
            }

            directory = fullPath;
        }

        IReadOnlyList<string> candidates = FindInDirectory(directory);

        return candidates.Count switch
        {
            0 => new SolutionResolution.NotFound($"No .sln or .slnx file found in {directory}"),
            1 => new SolutionResolution.Found(candidates[0]),
            _ => new SolutionResolution.Ambiguous(candidates),
        };
    }

    /// <summary>Lists solution files directly inside <paramref name="directory"/> (non-recursive).</summary>
    public static IReadOnlyList<string> FindInDirectory(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Patterns
            .SelectMany(pattern => Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly))
            // "*.sln" also matches ".slnx" on Windows (8.3 short-name quirk), so filter exactly.
            .Where(IsSolutionFile)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsSolutionFile(string path) =>
        Path.GetExtension(path).ToLowerInvariant() is ".sln" or ".slnx";
}
