using NuGet.Versioning;

namespace NugetScope.Core.Analysis;

/// <summary>Orders version strings semantically (9.0.0 before 10.0.0). Unparseable strings sort last, ordinally.</summary>
public sealed class VersionStringComparer : IComparer<string?>
{
    public static VersionStringComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        bool xParsed = NuGetVersion.TryParse(x, out NuGetVersion? xVersion);
        bool yParsed = NuGetVersion.TryParse(y, out NuGetVersion? yVersion);

        return (xParsed, yParsed) switch
        {
            (true, true) => VersionComparer.Default.Compare(xVersion, yVersion),
            (true, false) => -1,
            (false, true) => 1,
            _ => StringComparer.OrdinalIgnoreCase.Compare(x, y),
        };
    }
}
