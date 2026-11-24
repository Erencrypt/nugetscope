using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Tui.Theming;
using static NugetScope.Tui.Formatting.DetailSections;

namespace NugetScope.Tui.Formatting;

/// <summary>Coloured plain-text details for one package, for the detail pane.</summary>
internal static class PackageDetailFormatter
{
    public static IReadOnlyList<DetailLine> Format(PackageSummary package, PackageChecks completedChecks)
    {
        var lines = new List<DetailLine>();
        AddHeading(lines, package.Id);
        lines.AddRange(
        [
            Field("Status", StatusFormatter.FormatStatus(package, completedChecks), Palette.ToneFor(package.Status, completedChecks)),
            Field("In use", string.Join(", ", package.ResolvedVersions), package.HasVersionMismatch ? DetailTone.Mismatch : DetailTone.Normal),
            Field("Latest", StatusFormatter.FormatLatest(package, completedChecks)),
        ]);

        AppendDeprecation(lines, package.Deprecation);
        AppendVulnerabilities(lines, package);
        AppendMismatch(lines, package);
        AppendProjects(lines, package);
        AppendPossiblyUnused(lines, package);
        AddUncheckedNote(lines, completedChecks);

        return lines;
    }

    private static void AppendDeprecation(List<DetailLine> lines, PackageDeprecation? deprecation)
    {
        if (deprecation is null)
        {
            return;
        }

        string reasons = deprecation.Reasons.Count > 0 ? string.Join(", ", deprecation.Reasons) : "no reason given";
        lines.Add(Field("Deprecated", reasons, DetailTone.Deprecated));

        if (deprecation.AlternativePackageId is not null)
        {
            lines.Add(Field("Use instead", $"{deprecation.AlternativePackageId} {deprecation.AlternativeVersionRange}".TrimEnd()));
        }
    }

    private static void AppendVulnerabilities(List<DetailLine> lines, PackageSummary package)
    {
        if (package.Vulnerabilities.Count == 0)
        {
            return;
        }

        var rows = package.Vulnerabilities
            .Select(vulnerability => (IReadOnlyList<string>)
            [
                vulnerability.Severity.ToString(),
                string.Join(", ", package.Usages
                    .Where(u => u.Vulnerabilities.Contains(vulnerability))
                    .Select(u => u.ResolvedVersion)
                    .Distinct()),
                vulnerability.AdvisoryUrl?.ToString() ?? "(no advisory link)",
            ])
            .ToList();

        AddTable(lines, "Vulnerabilities", ["Severity", "Affects", "Advisory"], rows, _ => DetailTone.Vulnerable);
    }

    // Which projects sit on which version, highest first; the lower versions are the ones to align.
    private static void AppendMismatch(List<DetailLine> lines, PackageSummary package)
    {
        if (!package.HasVersionMismatch)
        {
            return;
        }

        IReadOnlyList<PackageVersionGroup> groups = package.VersionGroups;
        var rows = groups
            .Select((group, index) => (IReadOnlyList<string>)
            [
                group.Version,
                index == 0 ? "highest" : "behind",
                string.Join(", ", group.Projects.Select(p => p.Name)),
            ])
            .ToList();

        AddTable(
            lines,
            $"Version mismatch ({groups.Count} versions)",
            ["Version", "", "Projects"],
            rows,
            rowIndex => rowIndex == 0 ? DetailTone.Normal : DetailTone.Mismatch);
    }

    private static void AppendProjects(List<DetailLine> lines, PackageSummary package)
    {
        var rows = package.Usages
            .Select(usage => (IReadOnlyList<string>)
            [
                usage.Project.Name,
                FormatVersion(usage),
                string.Join(", ", usage.TargetFrameworks),
                FormatNotes(package, usage),
            ])
            .ToList();

        AddTable(
            lines,
            $"Projects ({package.ProjectCount})",
            ["Project", "Version", "Frameworks", "Notes"],
            rows,
            rowIndex => ToneFor(package, package.Usages[rowIndex]));
    }

    private static void AppendPossiblyUnused(List<DetailLine> lines, PackageSummary package)
    {
        var projects = package.Usages.Where(u => u.IsPossiblyUnused).Select(u => u.Project.Name).Distinct().ToList();
        if (projects.Count == 0)
        {
            return;
        }

        lines.Add(new DetailLine(""));
        lines.Add(new DetailLine($"Possibly unused in {string.Join(", ", projects)}: none of its namespaces appear in that code.", DetailTone.Unused));
        lines.Add(new DetailLine("Reflection, DI assembly scanning or configuration-based loading can't be detected; check before removing.", DetailTone.Muted));
    }

    private static DetailTone ToneFor(PackageSummary package, PackageUsage usage) =>
        Palette.ToneFor(usage) is DetailTone.Normal && IsBehind(package, usage) ? DetailTone.Mismatch : Palette.ToneFor(usage);

    private static bool IsBehind(PackageSummary package, PackageUsage usage) =>
        package.HasVersionMismatch
        && !string.Equals(usage.ResolvedVersion, package.ResolvedVersions[^1], StringComparison.OrdinalIgnoreCase);

    // Show the requested version only when it differs from what was resolved (floating versions, ranges).
    private static string FormatVersion(PackageUsage usage)
    {
        string resolved = usage.ResolvedVersion ?? "?";

        return usage.RequestedVersion is { } requested && !requested.Equals(resolved, StringComparison.OrdinalIgnoreCase)
            ? $"{requested} -> {resolved}"
            : resolved;
    }

    private static string FormatNotes(PackageSummary package, PackageUsage usage)
    {
        var notes = new List<string>();

        if (IsBehind(package, usage))
        {
            notes.Add($"behind {package.ResolvedVersions[^1]}");
        }

        if (usage.IsTransitive)
        {
            notes.Add("transitive");
        }

        if (usage.IsVulnerable)
        {
            notes.Add("vulnerable");
        }

        if (usage.IsDeprecated)
        {
            notes.Add("deprecated");
        }

        if (usage.IsOutdated)
        {
            notes.Add($"latest {usage.LatestVersion}");
        }

        if (usage.IsPossiblyUnused)
        {
            notes.Add("possibly unused");
        }

        return string.Join(", ", notes);
    }
}
