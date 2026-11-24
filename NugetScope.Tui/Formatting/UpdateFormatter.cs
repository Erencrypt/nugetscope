using System.Text;
using NuGet.Versioning;
using NugetScope.Core.Analysis;
using NugetScope.Core.Solutions;
using NugetScope.Core.Updates;

namespace NugetScope.Tui.Formatting;

/// <summary>Texts for the update dialogs: what will be updated, the version list, and the plan to confirm.</summary>
internal static class UpdateFormatter
{
    public static string DescribeScope(PackageSummary package, IReadOnlyCollection<ProjectInfo> projects)
    {
        string scope = projects.Count == package.ProjectCount
            ? $"Updates all {projects.Count} project(s) using {package.Id}."
            : $"Updates {package.Id} in {string.Join(", ", projects.Select(p => p.Name))}.";

        var inUse = package.VersionGroups
            .Select(g => (g.Version, Projects: g.Projects.Where(projects.Contains).ToList()))
            .Where(g => g.Projects.Count > 0)
            .Select(g => $"{g.Version} ({string.Join(", ", g.Projects.Select(p => p.Name))})");

        return $"{scope}\nIn use: {string.Join("; ", inUse)}";
    }

    /// <summary>The newest version reported by the outdated check for these projects, if any.</summary>
    public static string? LatestFor(PackageSummary package, IReadOnlyCollection<ProjectInfo> projects) =>
        package.Usages
            .Where(u => projects.Contains(u.Project))
            .Select(u => u.LatestVersion)
            .OfType<string>()
            .Max(VersionStringComparer.Instance);

    /// <summary>One line per version: the version plus tags such as "latest" or "in use".</summary>
    public static IReadOnlyList<string> VersionLabels(IReadOnlyList<string> versions, IReadOnlyCollection<string> inUse, string? latest)
    {
        int width = versions.Max(v => v.Length);

        return versions.Select(version =>
        {
            var tags = new List<string>();
            if (string.Equals(version, latest, StringComparison.OrdinalIgnoreCase))
            {
                tags.Add("latest");
            }

            if (inUse.Contains(version, StringComparer.OrdinalIgnoreCase))
            {
                tags.Add("in use");
            }

            if (IsPrerelease(version))
            {
                tags.Add("prerelease");
            }

            return $"{version.PadRight(width)}  {string.Join(", ", tags)}".TrimEnd();
        }).ToList();
    }

    /// <summary>The latest version from the scan when listed, otherwise the newest stable version.</summary>
    public static int DefaultVersionIndex(IReadOnlyList<string> versions, string? latest)
    {
        int index = latest is null ? -1 : versions.ToList().FindIndex(v => string.Equals(v, latest, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            index = versions.ToList().FindIndex(v => !IsPrerelease(v));
        }

        return Math.Max(index, 0);
    }

    public static string FormatPlan(PackageUpdatePlan plan, string solutionDirectory)
    {
        var text = new StringBuilder();

        if (plan.HasChanges)
        {
            text.AppendLine($"Update {plan.PackageId} to {plan.TargetVersion}. These files change:");

            foreach (PlannedEdit edit in plan.Edits)
            {
                text.AppendLine()
                    .AppendLine(Path.GetRelativePath(solutionDirectory, edit.Declaration.FilePath))
                    .AppendLine($"  {edit.Declaration.Kind}: {edit.OldValue} → {edit.NewValue}")
                    .AppendLine($"  Projects: {string.Join(", ", edit.AffectedProjects.Select(p => p.Name))}");
            }
        }
        else
        {
            text.AppendLine($"Nothing to change for {plan.PackageId} {plan.TargetVersion}.");
        }

        if (plan.Notes.Count > 0)
        {
            text.AppendLine().AppendLine("Not changed:");
            foreach (string note in plan.Notes)
            {
                text.AppendLine($"  {note}");
            }
        }

        return text.ToString().TrimEnd();
    }

    private static bool IsPrerelease(string version) =>
        NuGetVersion.TryParse(version, out NuGetVersion? parsed) && parsed.IsPrerelease;
}
