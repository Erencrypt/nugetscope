using NugetScope.Core.Solutions;

namespace NugetScope.Core.Analysis;

/// <summary>How one project uses a package. Target frameworks that share the same versions are merged.</summary>
public sealed record PackageUsage(
    ProjectInfo Project,
    IReadOnlyList<string> TargetFrameworks,
    string? RequestedVersion,
    string? ResolvedVersion,
    bool IsTransitive);
