using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;

namespace NugetScope.Tui.Formatting;

/// <summary>Short status/latest texts for table cells. Never claims "up to date" for checks that didn't complete.</summary>
internal static class StatusFormatter
{
    public const string Unknown = "?";

    public static string FormatStatus(PackageSummary package, PackageChecks completedChecks)
    {
        var parts = new List<string>();
        PackageStatus status = package.Status;

        if (status.HasFlag(PackageStatus.Vulnerable))
        {
            parts.Add($"vulnerable ({package.HighestSeverity?.ToString().ToLowerInvariant()})");
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

        if (parts.Count > 0)
        {
            return string.Join(", ", parts);
        }

        return completedChecks == PackageChecks.All ? "up to date" : Unknown;
    }

    public static string FormatLatest(PackageSummary package, PackageChecks completedChecks)
    {
        if (package.LatestVersion is not null)
        {
            return package.LatestVersion;
        }

        // Nothing reported as outdated: the highest version in use is the latest, if the check actually ran.
        return completedChecks.HasFlag(PackageChecks.Outdated) && package.ResolvedVersions.Count > 0
            ? package.ResolvedVersions[^1]
            : Unknown;
    }

    public static string Describe(PackageStatusFilter filter) => filter switch
    {
        PackageStatusFilter.All => "All",
        PackageStatusFilter.AnyIssue => "Any issue",
        PackageStatusFilter.Vulnerable => "Vulnerable",
        PackageStatusFilter.Outdated => "Outdated",
        PackageStatusFilter.Deprecated => "Deprecated",
        PackageStatusFilter.Mismatch => "Mismatch",
        _ => filter.ToString(),
    };
}
