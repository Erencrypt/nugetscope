namespace NugetScope.Core.Packages;

/// <param name="Projects">One entry per solution project, in solution order.</param>
/// <param name="Problems">Problems not tied to a specific project.</param>
/// <param name="CompletedChecks">
/// Checks that ran successfully. A package without a status flag is only known to be fine
/// for the checks listed here; the rest are unknown.
/// </param>
public sealed record PackageScanResult(
    IReadOnlyList<ProjectPackages> Projects,
    IReadOnlyList<PackageProblem> Problems,
    PackageChecks CompletedChecks = PackageChecks.None);
