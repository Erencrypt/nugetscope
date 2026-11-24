using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;
using NugetScope.Core.Reporting;
using NugetScope.Core.Solutions;

namespace NugetScope.Tests.Reporting;

public sealed class FailurePolicyTests
{
    private static readonly ProjectInfo A = new("A", Path.GetFullPath("C:/repo/A/A.csproj"), null);
    private static readonly ProjectInfo B = new("B", Path.GetFullPath("C:/repo/B/B.csproj"), null);

    [Fact]
    public void Fails_on_a_matching_package_and_ignores_other_statuses()
    {
        var packages = new[]
        {
            new PackageSummary("V", [Usage(A, "1.0.0") with { Vulnerabilities = [new PackageVulnerability(VulnerabilitySeverity.High, null)] }]),
            new PackageSummary("O", [Usage(A, "1.0.0") with { LatestVersion = "2.0.0" }]),
        };

        FailureEvaluation evaluation = FailurePolicy.Evaluate(PackageStatus.Vulnerable, packages, Scan(PackageChecks.All));

        Assert.True(evaluation.Failed);
        Assert.Equal("V", Assert.Single(evaluation.Matches).Id);
    }

    [Fact]
    public void Missing_checks_and_broken_projects_leave_the_result_incomplete_but_mismatch_needs_no_check()
    {
        var packages = new[] { new PackageSummary("M", [Usage(A, "1.0.0"), Usage(B, "2.0.0")]) };
        PackageScanResult scan = Scan(
            PackageChecks.Outdated,
            new ProjectPackages(B, [], [new PackageProblem(ProblemLevel.Error, "restore failed", "NU1101")]));

        FailureEvaluation mismatchOnly = FailurePolicy.Evaluate(PackageStatus.Mismatch, packages, Scan(PackageChecks.None));
        FailureEvaluation vulnerable = FailurePolicy.Evaluate(PackageStatus.Vulnerable, [], scan);

        Assert.True(mismatchOnly.Failed);
        Assert.Empty(mismatchOnly.Gaps);
        Assert.False(vulnerable.Passed);
        Assert.Equal(2, vulnerable.Gaps.Count); // The vulnerable check, and project B.
    }

    private static PackageUsage Usage(ProjectInfo project, string version) =>
        new(project, ["net10.0"], version, version, IsTransitive: false);

    private static PackageScanResult Scan(PackageChecks completed, params ProjectPackages[] projects) =>
        new(projects, [], completed);
}
