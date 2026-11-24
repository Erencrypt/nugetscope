using NugetScope.Core.Packages;

namespace NugetScope.Core.Analysis;

/// <summary>One package across the whole solution.</summary>
public sealed record PackageSummary(string Id, IReadOnlyList<PackageUsage> Usages)
{
    /// <summary>Distinct resolved versions in use, lowest first.</summary>
    public IReadOnlyList<string> ResolvedVersions { get; } = Usages
        .Select(u => u.ResolvedVersion)
        .OfType<string>()
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order(VersionStringComparer.Instance)
        .ToList();

    public int ProjectCount { get; } = Usages.Select(u => u.Project).Distinct().Count();

    /// <summary>Highest newer version reported by the outdated check; null when nothing is outdated or it didn't run.</summary>
    public string? LatestVersion { get; } = Usages
        .Select(u => u.LatestVersion)
        .OfType<string>()
        .Max(VersionStringComparer.Instance);

    /// <summary>Distinct advisories across all versions in use, most severe first.</summary>
    public IReadOnlyList<PackageVulnerability> Vulnerabilities { get; } = Usages
        .SelectMany(u => u.Vulnerabilities)
        .Distinct()
        .OrderByDescending(v => v.Severity)
        .ToList();

    public PackageDeprecation? Deprecation { get; } = Usages
        .Select(u => u.Deprecation)
        .FirstOrDefault(d => d is not null);

    public VulnerabilitySeverity? HighestSeverity => Vulnerabilities.Count > 0 ? Vulnerabilities[0].Severity : null;

    /// <summary>More than one resolved version is in use across the solution.</summary>
    public bool HasVersionMismatch => ResolvedVersions.Count > 1;

    /// <summary>Projects per resolved version, highest version first; the basis of the mismatch view.</summary>
    public IReadOnlyList<PackageVersionGroup> VersionGroups => Usages
        .Where(u => u.ResolvedVersion is not null)
        .GroupBy(u => u.ResolvedVersion!, StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(g => g.Key, VersionStringComparer.Instance)
        .Select(g => new PackageVersionGroup(g.Key, g.Select(u => u.Project).Distinct().ToList()))
        .ToList();

    public PackageStatus Status =>
        (Usages.Any(u => u.IsOutdated) ? PackageStatus.Outdated : PackageStatus.None)
        | (Deprecation is not null ? PackageStatus.Deprecated : PackageStatus.None)
        | (Vulnerabilities.Count > 0 ? PackageStatus.Vulnerable : PackageStatus.None)
        | (HasVersionMismatch ? PackageStatus.Mismatch : PackageStatus.None);
}
