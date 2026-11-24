using NugetScope.Core.Solutions;

namespace NugetScope.Core.Packages;

/// <summary>Scan result for one project: its packages across all target frameworks, plus any problems.</summary>
public sealed record ProjectPackages(
    ProjectInfo Project,
    IReadOnlyList<PackageReference> Packages,
    IReadOnlyList<PackageProblem> Problems)
{
    /// <summary>True when package data for this project is missing or unreliable.</summary>
    public bool HasErrors => Problems.Any(p => p.Level == ProblemLevel.Error);
}
