namespace NugetScope.Core.Packages;

/// <param name="Reasons">E.g. <c>Legacy</c>, <c>CriticalBugs</c>, <c>Other</c>.</param>
/// <param name="AlternativePackageId">Suggested replacement, if the author named one.</param>
/// <param name="AlternativeVersionRange">Version range of the replacement, e.g. <c>&gt;= 0.0.0</c>.</param>
public sealed record PackageDeprecation(
    IReadOnlyList<string> Reasons,
    string? AlternativePackageId,
    string? AlternativeVersionRange);
