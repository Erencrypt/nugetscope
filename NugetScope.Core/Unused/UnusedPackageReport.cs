using NugetScope.Core.Analysis;
using NugetScope.Core.Solutions;

namespace NugetScope.Core.Unused;

/// <summary>Direct package references that no code in their project appears to use (best effort).</summary>
public sealed class UnusedPackageReport(
    IReadOnlyDictionary<ProjectInfo, IReadOnlySet<string>> possiblyUnused,
    IReadOnlyDictionary<ProjectInfo, string> skipped)
{
    public static UnusedPackageReport Empty { get; } = new(new Dictionary<ProjectInfo, IReadOnlySet<string>>(), new Dictionary<ProjectInfo, string>());

    /// <summary>Projects that could not be checked, and why.</summary>
    public IReadOnlyDictionary<ProjectInfo, string> Skipped { get; } = skipped;

    public bool IsPossiblyUnused(ProjectInfo project, string packageId) =>
        possiblyUnused.TryGetValue(project, out IReadOnlySet<string>? ids) && ids.Contains(packageId);

    /// <summary>The summaries with <see cref="PackageUsage.IsPossiblyUnused"/> set on the flagged direct usages.</summary>
    public IReadOnlyList<PackageSummary> Apply(IReadOnlyList<PackageSummary> packages) => packages
        .Select(package => package.Usages.Any(u => IsFlagged(package, u))
            ? new PackageSummary(
                package.Id,
                package.Usages.Select(u => IsFlagged(package, u) ? u with { IsPossiblyUnused = true } : u).ToList())
            : package)
        .ToList();

    private bool IsFlagged(PackageSummary package, PackageUsage usage) =>
        !usage.IsTransitive && IsPossiblyUnused(usage.Project, package.Id);
}
