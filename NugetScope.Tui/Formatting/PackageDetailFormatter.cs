using System.Text;
using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;

namespace NugetScope.Tui.Formatting;

/// <summary>Plain-text details for one package, for the detail pane.</summary>
internal static class PackageDetailFormatter
{
    public static string Format(PackageSummary package, PackageChecks completedChecks)
    {
        var text = new StringBuilder();

        text.AppendLine(package.Id);
        text.AppendLine(new string('─', package.Id.Length));
        text.AppendLine($"Status:   {StatusFormatter.FormatStatus(package, completedChecks)}");
        text.AppendLine($"In use:   {string.Join(", ", package.ResolvedVersions)}");
        text.AppendLine($"Latest:   {StatusFormatter.FormatLatest(package, completedChecks)}");

        AppendVulnerabilities(text, package);
        AppendDeprecation(text, package.Deprecation);
        AppendUsages(text, package);
        AppendUncheckedNote(text, completedChecks);

        return text.ToString();
    }

    private static void AppendVulnerabilities(StringBuilder text, PackageSummary package)
    {
        if (package.Vulnerabilities.Count == 0)
        {
            return;
        }

        text.AppendLine();
        text.AppendLine("Vulnerabilities:");

        foreach (PackageVulnerability vulnerability in package.Vulnerabilities)
        {
            var affectedVersions = package.Usages
                .Where(u => u.Vulnerabilities.Contains(vulnerability))
                .Select(u => u.ResolvedVersion)
                .Distinct();

            text.AppendLine($"  {vulnerability.Severity,-8} {vulnerability.AdvisoryUrl?.ToString() ?? "(no advisory link)"}");
            text.AppendLine($"           affects {string.Join(", ", affectedVersions)}");
        }
    }

    private static void AppendDeprecation(StringBuilder text, PackageDeprecation? deprecation)
    {
        if (deprecation is null)
        {
            return;
        }

        text.AppendLine();
        text.AppendLine($"Deprecated: {(deprecation.Reasons.Count > 0 ? string.Join(", ", deprecation.Reasons) : "no reason given")}");

        if (deprecation.AlternativePackageId is not null)
        {
            text.AppendLine($"  Alternative: {deprecation.AlternativePackageId} {deprecation.AlternativeVersionRange}".TrimEnd());
        }
    }

    private static void AppendUsages(StringBuilder text, PackageSummary package)
    {
        text.AppendLine();
        text.AppendLine($"Projects ({package.ProjectCount}):");

        int nameWidth = package.Usages.Max(u => u.Project.Name.Length);
        int versionWidth = package.Usages.Max(u => FormatVersion(u).Length);

        foreach (PackageUsage usage in package.Usages)
        {
            string flags = FormatUsageFlags(usage);

            text.Append("  ")
                .Append(usage.Project.Name.PadRight(nameWidth))
                .Append("  ")
                .Append(FormatVersion(usage).PadRight(versionWidth))
                .Append("  ")
                .Append(string.Join(", ", usage.TargetFrameworks))
                .AppendLine(flags.Length > 0 ? $"  [{flags}]" : "");
        }
    }

    private static void AppendUncheckedNote(StringBuilder text, PackageChecks completedChecks)
    {
        PackageChecks missing = PackageChecks.All & ~completedChecks;
        if (missing == PackageChecks.None)
        {
            return;
        }

        text.AppendLine();
        text.AppendLine($"Not checked: {missing.ToString().ToLowerInvariant()} (see Ctrl+P for problems).");
    }

    // Show the requested version only when it differs from what was resolved (floating versions, ranges).
    private static string FormatVersion(PackageUsage usage)
    {
        string resolved = usage.ResolvedVersion ?? "?";

        if (usage.IsTransitive)
        {
            return $"{resolved} (transitive)";
        }

        return usage.RequestedVersion is { } requested && !requested.Equals(resolved, StringComparison.OrdinalIgnoreCase)
            ? $"{requested} -> {resolved}"
            : resolved;
    }

    private static string FormatUsageFlags(PackageUsage usage)
    {
        var flags = new List<string>();

        if (usage.IsVulnerable)
        {
            flags.Add("vulnerable");
        }

        if (usage.IsDeprecated)
        {
            flags.Add("deprecated");
        }

        if (usage.IsOutdated)
        {
            flags.Add($"latest {usage.LatestVersion}");
        }

        return string.Join(", ", flags);
    }
}
