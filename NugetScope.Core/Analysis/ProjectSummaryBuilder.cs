namespace NugetScope.Core.Analysis;

/// <summary>Regroups package summaries per project, so both groupings share the same analysis.</summary>
public static class ProjectSummaryBuilder
{
    public static IReadOnlyList<ProjectSummary> Build(IEnumerable<PackageSummary> packages)
    {
        return packages
            .SelectMany(package => package.Usages.Select(usage => (Package: package, Usage: usage)))
            .GroupBy(x => x.Usage.Project)
            .Select(project => new ProjectSummary(
                project.Key,
                project
                    .GroupBy(x => x.Package)
                    .Select(group => new ProjectPackage(group.Key, group.Select(x => x.Usage).ToList()))
                    .OrderBy(p => p.Package.Id, StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .OrderBy(project => project.Project.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
