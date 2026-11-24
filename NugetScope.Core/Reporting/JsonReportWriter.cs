using System.Text.Json;
using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Core.Reporting.Json;
using NugetScope.Core.Scanning;
using NugetScope.Core.Unused;

namespace NugetScope.Core.Reporting;

/// <summary>The machine-readable <c>--json</c> report: summary counts, every package with its usages, and all problems.</summary>
public static class JsonReportWriter
{
    /// <param name="unused">Result of the possibly-unused check, when it ran.</param>
    public static string Write(SolutionScan scan, IReadOnlyList<PackageSummary> packages, UnusedPackageReport? unused = null)
    {
        var document = new ScanReportDocument
        {
            Solution = scan.Solution.FilePath,
            CompletedChecks = [.. PackageStatusNames.ToNames(scan.Packages.CompletedChecks)],
            Summary = new ReportSummaryJson
            {
                Projects = scan.Solution.Projects.Count,
                Packages = packages.Count,
                Vulnerable = packages.Count(p => p.Status.HasFlag(PackageStatus.Vulnerable)),
                Deprecated = packages.Count(p => p.Status.HasFlag(PackageStatus.Deprecated)),
                Outdated = packages.Count(p => p.Status.HasFlag(PackageStatus.Outdated)),
                Mismatch = packages.Count(p => p.Status.HasFlag(PackageStatus.Mismatch)),
                PossiblyUnused = packages.Count(p => p.Status.HasFlag(PackageStatus.PossiblyUnused)),
            },
            Packages = [.. packages.Select(ToJson)],
            Projects = [.. scan.Packages.Projects.Select(p => new ReportProjectJson
            {
                Name = p.Project.Name,
                Path = p.Project.FilePath,
                SolutionFolder = p.Project.SolutionFolder,
                UnusedCheckSkipped = unused?.Skipped.GetValueOrDefault(p.Project),
                Problems = [.. p.Problems.Select(ToJson)],
            })],
            Problems = [.. scan.Packages.Problems.Select(ToJson)],
        };

        return JsonSerializer.Serialize(document, ScanReportJsonContext.Default.ScanReportDocument);
    }

    private static ReportPackageJson ToJson(PackageSummary package) => new()
    {
        Id = package.Id,
        Status = [.. PackageStatusNames.ToNames(package.Status)],
        Versions = [.. package.ResolvedVersions],
        Latest = package.LatestVersion,
        Transitive = package.IsTransitiveOnly,
        Deprecation = package.Deprecation is { } deprecation
            ? new ReportDeprecationJson
            {
                Reasons = [.. deprecation.Reasons],
                AlternativePackage = deprecation.AlternativePackageId,
                AlternativeVersionRange = deprecation.AlternativeVersionRange,
            }
            : null,
        Vulnerabilities = [.. package.Vulnerabilities.Select(v => new ReportVulnerabilityJson
        {
            Severity = v.Severity.ToString().ToLowerInvariant(),
            AdvisoryUrl = v.AdvisoryUrl?.ToString(),
        })],
        Projects = [.. package.Usages.Select(u => new ReportUsageJson
        {
            Project = u.Project.Name,
            Frameworks = [.. u.TargetFrameworks],
            Requested = u.RequestedVersion,
            Resolved = u.ResolvedVersion,
            Latest = u.LatestVersion,
            Transitive = u.IsTransitive,
            PossiblyUnused = u.IsPossiblyUnused,
        })],
    };

    private static ReportProblemJson ToJson(PackageProblem problem) => new()
    {
        Level = problem.Level.ToString().ToLowerInvariant(),
        Code = problem.Code,
        Message = problem.Message,
    };
}
