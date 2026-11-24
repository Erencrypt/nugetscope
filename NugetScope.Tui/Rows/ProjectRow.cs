using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;

namespace NugetScope.Tui.Rows;

/// <summary>A project, expandable to its packages (by-project grouping).</summary>
internal sealed class ProjectRow(ProjectSummary project) : DashboardRow
{
    public ProjectSummary Project => project;

    public override string Name => project.Project.Name;
    public override IReadOnlyList<string> ResolvedVersions => [];
    public override string? LatestVersion => null;
    public override PackageStatus Status => project.Status;
    public override VulnerabilitySeverity? HighestSeverity => project.HighestSeverity;
    public override int? Count => project.Packages.Count;
    public override IReadOnlyList<DashboardRow> Children { get; } =
        project.Packages.Select(p => new ProjectPackageRow(p)).ToList();
    public override PackageSummary? Package => null;
}
