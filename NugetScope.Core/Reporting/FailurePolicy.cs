using NugetScope.Core.Analysis;
using NugetScope.Core.Packages;

namespace NugetScope.Core.Reporting;

/// <summary>
/// The CI gate behind <c>--fail-on</c>: fails when a package has one of the given statuses, and reports the result as
/// incomplete when the data can't prove the absence of those statuses (a check didn't run, a project wasn't scanned).
/// </summary>
public static class FailurePolicy
{
    public static FailureEvaluation Evaluate(PackageStatus failOn, IReadOnlyList<PackageSummary> packages, PackageScanResult scan)
    {
        var matches = packages.Where(p => (p.Status & failOn) != PackageStatus.None).ToList();

        var gaps = PackageStatusNames.Each(failOn)
            .Where(status => PackageStatusNames.RequiredCheck(status) is var check
                && check != PackageChecks.None
                && !scan.CompletedChecks.HasFlag(check))
            .Select(status => $"The {PackageStatusNames.Name(status)} check did not complete.")
            .Concat(scan.Projects
                .Where(p => p.HasErrors)
                .Select(p => $"{p.Project.Name} could not be scanned (see its problems)."))
            .ToList();

        return new FailureEvaluation(failOn, matches, gaps);
    }
}
