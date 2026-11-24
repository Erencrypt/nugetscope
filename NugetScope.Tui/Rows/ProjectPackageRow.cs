using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;

namespace NugetScope.Tui.Rows;

/// <summary>A package as one project uses it (child of a <see cref="ProjectRow"/>).</summary>
internal sealed class ProjectPackageRow(ProjectPackage package) : DashboardRow
{
    public override string Name => package.Package.Id;
    public override IReadOnlyList<string> ResolvedVersions => package.ResolvedVersions;
    public override string? LatestVersion => package.LatestVersion;
    public override PackageStatus Status => package.Status;
    public override VulnerabilitySeverity? HighestSeverity => package.HighestSeverity;
    public override int? Count => null;
    public override PackageSummary Package => package.Package;
    public override IReadOnlyList<ProjectInfo> UpdateProjects { get; } = package.Usages.Select(u => u.Project).Distinct().ToList();
}
