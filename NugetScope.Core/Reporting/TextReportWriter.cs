using System.Text;
using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Core.Scanning;

namespace NugetScope.Core.Reporting;

/// <summary>A short plain-text report for CI logs: packages with issues grouped by status, problems, and the gate result.</summary>
public static class TextReportWriter
{
    public static string Write(SolutionScan scan, IReadOnlyList<PackageSummary> packages, FailureEvaluation? evaluation)
    {
        var text = new StringBuilder();
        PackageChecks completed = scan.Packages.CompletedChecks;

        text.AppendLine($"{scan.Solution.Name}: {scan.Solution.Projects.Count} projects, {packages.Count} packages");

        foreach (PackageStatus status in PackageStatusNames.Each(PackageStatusNames.AnyIssue))
        {
            string name = PackageStatusNames.Name(status);
            PackageChecks check = PackageStatusNames.RequiredCheck(status);

            if (check != PackageChecks.None && !completed.HasFlag(check))
            {
                text.AppendLine().AppendLine($"{name}: not checked");
                continue;
            }

            var flagged = packages.Where(p => p.Status.HasFlag(status)).ToList();
            if (flagged.Count == 0)
            {
                continue;
            }

            text.AppendLine().AppendLine($"{name} ({flagged.Count})");
            foreach (PackageSummary package in flagged)
            {
                text.AppendLine($"  {package.Id} {Describe(package, status)} [{string.Join(", ", package.Usages.Select(u => u.Project.Name).Distinct())}]");
            }
        }

        AppendProblems(text, scan.Packages);

        if (evaluation is not null)
        {
            text.AppendLine();
            foreach (string gap in evaluation.Gaps)
            {
                text.AppendLine(gap);
            }

            text.AppendLine(evaluation.Describe());
        }

        return text.ToString();
    }

    private static string Describe(PackageSummary package, PackageStatus status)
    {
        string versions = string.Join(", ", package.ResolvedVersions);

        return status switch
        {
            PackageStatus.Vulnerable => $"{versions} ({package.HighestSeverity?.ToString().ToLowerInvariant()})",
            PackageStatus.Outdated => $"{versions} -> {package.LatestVersion}",
            PackageStatus.Deprecated when package.Deprecation?.AlternativePackageId is { } alternative => $"{versions} (use {alternative})",
            _ => versions,
        };
    }

    private static void AppendProblems(StringBuilder text, PackageScanResult scan)
    {
        var problems = scan.Problems.Select(p => ("Solution", p))
            .Concat(scan.Projects.SelectMany(project => project.Problems.Select(p => (project.Project.Name, p))))
            .ToList();

        if (problems.Count == 0)
        {
            return;
        }

        text.AppendLine().AppendLine($"problems ({problems.Count})");
        foreach ((string scope, PackageProblem problem) in problems)
        {
            string code = problem.Code is null ? "" : $" {problem.Code}";
            text.AppendLine($"  [{problem.Level}{code}] {scope}: {problem.Message}");
        }
    }
}
