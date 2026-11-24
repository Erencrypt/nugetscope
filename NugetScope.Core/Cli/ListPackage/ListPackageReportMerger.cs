using NugetScope.Core.IO;
using NugetScope.Core.Packages;

namespace NugetScope.Core.Cli.ListPackage;

/// <summary>
/// Folds the status-only reports (<c>--outdated</c>, <c>--deprecated</c>, <c>--vulnerable</c>) into the base package
/// list. Status reports only contain flagged packages, so they are matched by project, framework and package id.
/// </summary>
internal static class ListPackageReportMerger
{
    public static ListPackageReport Merge(ListPackageReport baseReport, IReadOnlyList<ListPackageReport> statusReports)
    {
        var status = IndexStatus(statusReports);

        var projects = new Dictionary<string, ListedProject>(PathNormalizer.Comparer);

        foreach ((string path, ListedProject project) in baseReport.Projects)
        {
            var packages = project.Packages
                .Select(package => ApplyStatus(package, status.GetValueOrDefault(new PackageKey(path, package.TargetFramework, package.Id))))
                .ToList();

            projects[path] = project with { Packages = packages };
        }

        // Problems are repeated by every call (e.g. "no assets file" for the same project); keep each once.
        foreach (ListPackageReport report in statusReports)
        {
            foreach ((string path, ListedProject statusProject) in report.Projects)
            {
                projects[path] = projects.TryGetValue(path, out ListedProject? existing)
                    ? existing with { Problems = existing.Problems.Concat(statusProject.Problems).Distinct().ToList() }
                    : statusProject with { Packages = [] };
            }
        }

        var solutionProblems = baseReport.Problems
            .Concat(statusReports.SelectMany(r => r.Problems))
            .Distinct()
            .ToList();

        return new ListPackageReport(projects, solutionProblems);
    }

    private static Dictionary<PackageKey, PackageReference> IndexStatus(IReadOnlyList<ListPackageReport> statusReports)
    {
        var index = new Dictionary<PackageKey, PackageReference>(PackageKey.Comparer);

        foreach (ListPackageReport report in statusReports)
        {
            foreach ((string path, ListedProject project) in report.Projects)
            {
                foreach (PackageReference package in project.Packages)
                {
                    var key = new PackageKey(path, package.TargetFramework, package.Id);
                    index[key] = index.TryGetValue(key, out PackageReference? existing)
                        ? ApplyStatus(existing, package)
                        : package;
                }
            }
        }

        return index;
    }

    private static PackageReference ApplyStatus(PackageReference package, PackageReference? status)
    {
        if (status is null)
        {
            return package;
        }

        return package with
        {
            LatestVersion = status.LatestVersion ?? package.LatestVersion,
            Deprecation = status.Deprecation ?? package.Deprecation,
            Vulnerabilities = status.Vulnerabilities.Count > 0 ? status.Vulnerabilities : package.Vulnerabilities,
        };
    }

    private readonly record struct PackageKey(string ProjectPath, string Framework, string Id)
    {
        public static IEqualityComparer<PackageKey> Comparer { get; } = new KeyComparer();

        private sealed class KeyComparer : IEqualityComparer<PackageKey>
        {
            public bool Equals(PackageKey x, PackageKey y) =>
                PathNormalizer.Comparer.Equals(x.ProjectPath, y.ProjectPath)
                && StringComparer.OrdinalIgnoreCase.Equals(x.Framework, y.Framework)
                && StringComparer.OrdinalIgnoreCase.Equals(x.Id, y.Id);

            public int GetHashCode(PackageKey key) => HashCode.Combine(
                PathNormalizer.Comparer.GetHashCode(key.ProjectPath),
                StringComparer.OrdinalIgnoreCase.GetHashCode(key.Framework),
                StringComparer.OrdinalIgnoreCase.GetHashCode(key.Id));
        }
    }
}
