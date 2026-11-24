using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;

namespace NugetScope.Core.Analysis;

/// <summary>
/// How one project uses a package. Target frameworks that share the same versions are merged.
/// Status data is per usage because vulnerabilities and deprecation apply to a specific version.
/// </summary>
public sealed record PackageUsage(
    ProjectInfo Project,
    IReadOnlyList<string> TargetFrameworks,
    string? RequestedVersion,
    string? ResolvedVersion,
    bool IsTransitive)
{
    /// <summary>Newer version reported by the outdated check; null when up to date or unchecked.</summary>
    public string? LatestVersion { get; init; }

    public PackageDeprecation? Deprecation { get; init; }

    public IReadOnlyList<PackageVulnerability> Vulnerabilities { get; init; } = [];

    /// <summary>Set by the unused check: no code in the project appears to use this direct reference.</summary>
    public bool IsPossiblyUnused { get; init; }

    public bool IsOutdated =>
        LatestVersion is not null && VersionStringComparer.Instance.Compare(LatestVersion, ResolvedVersion) > 0;

    public bool IsDeprecated => Deprecation is not null;

    public bool IsVulnerable => Vulnerabilities.Count > 0;
}
