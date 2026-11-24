using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Tui.Theming;
using static NugetScope.Tui.Formatting.DetailSections;

namespace NugetScope.Tui.Formatting;

/// <summary>Coloured plain-text details for one project, for the detail pane.</summary>
internal static class ProjectDetailFormatter
{
    public static IReadOnlyList<DetailLine> Format(
        ProjectSummary project,
        IReadOnlyList<PackageProblem> problems,
        PackageChecks completedChecks)
    {
        var lines = new List<DetailLine>();
        AddHeading(lines, project.Project.Name);
        lines.Add(Field(
            "Status",
            StatusFormatter.FormatStatus(project.Status, project.HighestSeverity, completedChecks),
            Palette.ToneFor(project.Status, completedChecks)));
        lines.Add(Field("Path", project.Project.FilePath));

        if (project.Project.SolutionFolder is { } folder)
        {
            lines.Add(Field("Folder", folder));
        }

        AppendProblems(lines, problems);
        AppendPackages(lines, project, completedChecks);
        AddUncheckedNote(lines, completedChecks);

        return lines;
    }

    private static void AppendProblems(List<DetailLine> lines, IReadOnlyList<PackageProblem> problems)
    {
        if (problems.Count == 0)
        {
            return;
        }

        var rows = problems
            .Select(problem => (IReadOnlyList<string>)[problem.Level.ToString(), problem.Code ?? "", problem.Message])
            .ToList();

        AddTable(
            lines,
            $"Problems ({problems.Count})",
            ["Level", "Code", "Message"],
            rows,
            rowIndex => problems[rowIndex].Level == ProblemLevel.Error ? DetailTone.Error : DetailTone.Warning);
    }

    private static void AppendPackages(List<DetailLine> lines, ProjectSummary project, PackageChecks completedChecks)
    {
        var rows = project.Packages
            .Select(package => (IReadOnlyList<string>)
            [
                package.Package.Id,
                string.Join(", ", package.ResolvedVersions),
                StatusFormatter.FormatLatest(package.LatestVersion, package.ResolvedVersions, completedChecks),
                StatusFormatter.FormatStatus(package.Status, package.HighestSeverity, completedChecks),
            ])
            .ToList();

        AddTable(
            lines,
            $"Packages ({project.Packages.Count})",
            ["Package", "Version", "Latest", "Status"],
            rows,
            rowIndex => Palette.ToneFor(project.Packages[rowIndex].Status, completedChecks));
    }
}
