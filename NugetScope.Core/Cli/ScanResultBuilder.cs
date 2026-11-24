using NugetScope.Core.Cli.ListPackage;
using NugetScope.Core.Cli.Restore;
using NugetScope.Core.IO;
using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;

namespace NugetScope.Core.Cli;

/// <summary>Maps parsed CLI output onto the solution's projects, attaching restore errors and other problems.</summary>
internal static class ScanResultBuilder
{
    public static PackageScanResult Build(
        SolutionInfo solution,
        ListPackageReport report,
        IReadOnlyList<RestoreDiagnostic> restoreDiagnostics,
        PackageChecks completedChecks,
        IReadOnlyList<PackageProblem> additionalProblems)
    {
        var restoreErrors = restoreDiagnostics.Where(d => d.Level == ProblemLevel.Error).ToList();
        var restoreErrorsByProject = restoreErrors
            .Where(d => d.FilePath is not null)
            .ToLookup(d => d.FilePath!, PathNormalizer.Comparer);

        var solutionProjectPaths = solution.Projects
            .Select(p => PathNormalizer.Normalize(p.FilePath))
            .ToHashSet(PathNormalizer.Comparer);

        var projects = solution.Projects
            .Select(project => BuildProject(project, report, restoreErrorsByProject))
            .ToList();

        // Restore errors not tied to a solution project (feed/auth errors, the .sln itself) are solution-wide.
        var solutionProblems = report.Problems
            .Concat(restoreErrors
                .Where(d => d.FilePath is null || !solutionProjectPaths.Contains(d.FilePath))
                .Select(d => d.ToProblem()))
            .Concat(additionalProblems)
            .ToList();

        return new PackageScanResult(projects, solutionProblems, completedChecks);
    }

    private static ProjectPackages BuildProject(
        ProjectInfo project,
        ListPackageReport report,
        ILookup<string, RestoreDiagnostic> restoreErrorsByProject)
    {
        string path = PathNormalizer.Normalize(project.FilePath);
        var problems = restoreErrorsByProject[path].Select(d => d.ToProblem()).ToList();

        if (report.Projects.TryGetValue(path, out ListedProject? listed))
        {
            problems.AddRange(listed.Problems);
            return new ProjectPackages(project, listed.Packages, problems);
        }

        if (problems.Count == 0)
        {
            problems.Add(new PackageProblem(ProblemLevel.Warning, "Project was not reported by 'dotnet list package'."));
        }

        return new ProjectPackages(project, [], problems);
    }
}
