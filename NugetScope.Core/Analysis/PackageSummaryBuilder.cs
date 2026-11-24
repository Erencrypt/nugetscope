using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;

namespace NugetScope.Core.Analysis;

/// <summary>Groups per-project scan results into one summary per package id.</summary>
public static class PackageSummaryBuilder
{
    /// <param name="includeTransitive">Include packages that are only pulled in transitively.</param>
    /// <param name="includeAutoReferenced">Include SDK-implicit packages (e.g. NETStandard.Library), which users can't update.</param>
    public static IReadOnlyList<PackageSummary> Build(
        PackageScanResult scan,
        bool includeTransitive = false,
        bool includeAutoReferenced = false)
    {
        return scan.Projects
            .SelectMany(project => project.Packages.Select(package => (project.Project, Package: package)))
            .Where(x => includeTransitive || !x.Package.IsTransitive)
            .Where(x => includeAutoReferenced || !x.Package.IsAutoReferenced)
            .GroupBy(x => x.Package.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => new PackageSummary(group.First().Package.Id, BuildUsages(group)))
            .OrderBy(summary => summary.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<PackageUsage> BuildUsages(IEnumerable<(ProjectInfo Project, PackageReference Package)> references)
    {
        return references
            .GroupBy(x => (x.Project, x.Package.RequestedVersion, x.Package.ResolvedVersion, x.Package.IsTransitive))
            .Select(group =>
            {
                var packages = group.Select(x => x.Package).ToList();

                return new PackageUsage(
                    group.Key.Project,
                    packages.Select(p => p.TargetFramework).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    group.Key.RequestedVersion,
                    group.Key.ResolvedVersion,
                    group.Key.IsTransitive)
                {
                    LatestVersion = packages.Select(p => p.LatestVersion).OfType<string>().Max(VersionStringComparer.Instance),
                    Deprecation = packages.Select(p => p.Deprecation).FirstOrDefault(d => d is not null),
                    Vulnerabilities = packages.SelectMany(p => p.Vulnerabilities).Distinct().ToList(),
                };
            })
            .OrderBy(usage => usage.Project.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(usage => usage.ResolvedVersion, VersionStringComparer.Instance)
            .ToList();
    }
}
