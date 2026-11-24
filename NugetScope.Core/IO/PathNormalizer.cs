namespace NugetScope.Core.IO;

/// <summary>Normalizes file paths so CLI output and solution entries can be matched.</summary>
public static class PathNormalizer
{
    /// <summary>Case-insensitive on Windows, ordinal elsewhere.</summary>
    public static StringComparer Comparer { get; } =
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    /// <summary>Converts mixed separators (e.g. <c>c:/repo\A.csproj</c>) to a full, OS-native path.</summary>
    public static string Normalize(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Path.GetFullPath(path.Replace('\\', Path.DirectorySeparatorChar));
    }
}
