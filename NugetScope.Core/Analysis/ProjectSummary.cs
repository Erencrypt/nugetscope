using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;

namespace NugetScope.Core.Analysis;

/// <summary>One project and the packages it references.</summary>
public sealed record ProjectSummary(ProjectInfo Project, IReadOnlyList<ProjectPackage> Packages)
{
    public PackageStatus Status => Packages.Aggregate(PackageStatus.None, (status, package) => status | package.Status);

    public VulnerabilitySeverity? HighestSeverity => Packages.Max(p => p.HighestSeverity);
}
