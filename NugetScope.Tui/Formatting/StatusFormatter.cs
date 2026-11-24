using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;

namespace NugetScope.Tui.Formatting;

/// <summary>Short status/latest texts for table cells. Never claims "up to date" for checks that didn't complete.</summary>
internal static class StatusFormatter
{
    public const string Unknown = "?";

    public static string FormatStatus(PackageSummary package, PackageChecks completedChecks) =>
        FormatStatus(package.Status, package.HighestSeverity, completedChecks);

    public static string FormatStatus(PackageStatus status, VulnerabilitySeverity? highestSeverity, PackageChecks completedChecks)
    {
        var parts = new List<string>();

        if (status.HasFlag(PackageStatus.Vulnerable))
        {
            parts.Add($"vulnerable ({highestSeverity?.ToString().ToLowerInvariant()})");
        }

        if (status.HasFlag(PackageStatus.Deprecated))
        {
            parts.Add("deprecated");
        }

        if (status.HasFlag(PackageStatus.Outdated))
        {
            parts.Add("outdated");
        }

        if (status.HasFlag(PackageStatus.Mismatch))
        {
            parts.Add("mismatch");
        }

        if (status.HasFlag(PackageStatus.PossiblyUnused))
        {
            parts.Add("possibly unused");
        }

        if (parts.Count > 0)
        {
            return string.Join(", ", parts);
        }

        return completedChecks == PackageChecks.All ? "up to date" : Unknown;
    }

    public static string FormatLatest(PackageSummary package, PackageChecks completedChecks) =>
        FormatLatest(package.LatestVersion, package.ResolvedVersions, completedChecks);

    /// <param name="resolvedVersions">Versions in use, lowest first.</param>
    public static string FormatLatest(string? latestVersion, IReadOnlyList<string> resolvedVersions, PackageChecks completedChecks)
    {
        if (latestVersion is not null)
        {
            return latestVersion;
        }

        // Nothing reported as outdated: the highest version in use is the latest, if the check actually ran.
        return completedChecks.HasFlag(PackageChecks.Outdated) && resolvedVersions.Count > 0
            ? resolvedVersions[^1]
            : Unknown;
    }

    public static string Describe(PackageGrouping grouping) => grouping switch
    {
        PackageGrouping.ByProject => "Projects",
        _ => "Packages",
    };

    public static string Describe(PackageStatusFilter filter) => filter switch
    {
        PackageStatusFilter.All => "All",
        PackageStatusFilter.AnyIssue => "Any issue",
        PackageStatusFilter.Vulnerable => "Vulnerable",
        PackageStatusFilter.Outdated => "Outdated",
        PackageStatusFilter.Deprecated => "Deprecated",
        PackageStatusFilter.Mismatch => "Mismatch",
        PackageStatusFilter.PossiblyUnused => "Possibly unused",
        _ => filter.ToString(),
    };
}
