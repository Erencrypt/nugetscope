using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Tui.Theming;

namespace NugetScope.Tui.Formatting;

/// <summary>Coloured plain-text details for one package, for the detail pane.</summary>
internal static class PackageDetailFormatter
{
    private const int LabelWidth = 12;

    public static IReadOnlyList<DetailLine> Format(PackageSummary package, PackageChecks completedChecks)
    {
        var lines = new List<DetailLine>
        {
            new(package.Id, DetailTone.Heading),
            new(new string('═', package.Id.Length), DetailTone.Heading),
            Field("Status", StatusFormatter.FormatStatus(package, completedChecks), Palette.ToneFor(package.Status, completedChecks)),
            Field("In use", string.Join(", ", package.ResolvedVersions), package.HasVersionMismatch ? DetailTone.Mismatch : DetailTone.Normal),
            Field("Latest", StatusFormatter.FormatLatest(package, completedChecks)),
        };

        AppendDeprecation(lines, package.Deprecation);
        AppendVulnerabilities(lines, package);
        AppendProjects(lines, package);
        AppendUncheckedNote(lines, completedChecks);

        return lines;
    }

    private static DetailLine Field(string label, string value, DetailTone tone = DetailTone.Normal) =>
        new(label.PadRight(LabelWidth) + value, tone);

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

        AppendTable(lines, "Vulnerabilities", ["Severity", "Affects", "Advisory"], rows, _ => DetailTone.Vulnerable);
    }

    private static void AppendProjects(List<DetailLine> lines, PackageSummary package)
    {
        var rows = package.Usages
            .Select(usage => (IReadOnlyList<string>)
            [
                usage.Project.Name,
                FormatVersion(usage),
                string.Join(", ", usage.TargetFrameworks),
                FormatNotes(usage),
            ])
            .ToList();

        AppendTable(
            lines,
            $"Projects ({package.ProjectCount})",
            ["Project", "Version", "Frameworks", "Notes"],
            rows,
            rowIndex => Palette.ToneFor(package.Usages[rowIndex]));
    }

    // TextTable yields the header, the rule, then one line per row, in that order.
    private static void AppendTable(
        List<DetailLine> lines,
        string title,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        Func<int, DetailTone> rowTone)
    {
        lines.Add(new DetailLine(""));
        lines.Add(new DetailLine(title, DetailTone.Heading));

        int index = 0;
        foreach (string line in TextTable.Render(headers, rows))
        {
            DetailTone tone = index < 2 ? DetailTone.Muted : rowTone(index - 2);
            lines.Add(new DetailLine(line, tone));
            index++;
        }
    }

    private static void AppendUncheckedNote(List<DetailLine> lines, PackageChecks completedChecks)
    {
        PackageChecks missing = PackageChecks.All & ~completedChecks;
        if (missing != PackageChecks.None)
        {
            lines.Add(new DetailLine(""));
            lines.Add(new DetailLine($"Not checked: {missing.ToString().ToLowerInvariant()} (Ctrl+P shows why).", DetailTone.Muted));
        }
    }

    // Show the requested version only when it differs from what was resolved (floating versions, ranges).
    private static string FormatVersion(PackageUsage usage)
    {
        string resolved = usage.ResolvedVersion ?? "?";

        return usage.RequestedVersion is { } requested && !requested.Equals(resolved, StringComparison.OrdinalIgnoreCase)
            ? $"{requested} -> {resolved}"
            : resolved;
    }

    private static string FormatNotes(PackageUsage usage)
    {
        var notes = new List<string>();

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

        return string.Join(", ", notes);
    }
}
