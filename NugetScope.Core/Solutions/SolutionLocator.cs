namespace NugetScope.Core.Solutions;

/// <summary>Finds solution files in a directory (non-recursive).</summary>
public static class SolutionLocator
{
    private static readonly string[] Patterns = ["*.sln", "*.slnx"];

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
            .Where(path => Path.GetExtension(path).ToLowerInvariant() is ".sln" or ".slnx")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
