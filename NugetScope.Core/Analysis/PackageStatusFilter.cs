namespace NugetScope.Core.Analysis;

/// <summary>Status filter choices, in the order the UI cycles through them.</summary>
public enum PackageStatusFilter
{
    All,
    AnyIssue,
    Vulnerable,
    Outdated,
    Deprecated,
    Mismatch,
}
