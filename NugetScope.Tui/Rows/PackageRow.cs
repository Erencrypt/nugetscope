using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;

namespace NugetScope.Tui.Rows;

/// <summary>A package across the whole solution (by-package grouping).</summary>
internal sealed class PackageRow(PackageSummary package) : DashboardRow
{
    public override string Name => package.IsTransitiveOnly ? $"{package.Id} (transitive)" : package.Id;
    public override IReadOnlyList<string> ResolvedVersions => package.ResolvedVersions;
    public override string? LatestVersion => package.LatestVersion;
    public override PackageStatus Status => package.Status;
    public override VulnerabilitySeverity? HighestSeverity => package.HighestSeverity;
    public override int? Count => package.ProjectCount;
    public override PackageSummary Package => package;
    public override IReadOnlyList<ProjectInfo> UpdateProjects { get; } =
        package.Usages.Where(u => !u.IsTransitive).Select(u => u.Project).Distinct().ToList();
}
