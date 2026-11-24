using NugetScope.Core.Packages;

namespace NugetScope.Core.Analysis;

/// <summary>One package as referenced by one project (several usages when target frameworks resolve differently).</summary>
/// <param name="Package">The package across the whole solution, for the cross-project picture.</param>
/// <param name="Usages">This project's usages of the package.</param>
public sealed record ProjectPackage(PackageSummary Package, IReadOnlyList<PackageUsage> Usages)
{
    /// <summary>Distinct resolved versions in this project, lowest first.</summary>
    public IReadOnlyList<string> ResolvedVersions { get; } = Usages
        .Select(u => u.ResolvedVersion)
        .OfType<string>()
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order(VersionStringComparer.Instance)
        .ToList();

    public string? LatestVersion { get; } = Usages
        .Select(u => u.LatestVersion)
        .OfType<string>()
        .Max(VersionStringComparer.Instance);

    public VulnerabilitySeverity? HighestSeverity { get; } = Usages
        .SelectMany(u => u.Vulnerabilities)
        .Select(v => (VulnerabilitySeverity?)v.Severity)
        .Max();

    /// <summary>Issues of this project's usages. Mismatch is solution-wide: set when the package has several versions anywhere.</summary>
    public PackageStatus Status =>
        (Usages.Any(u => u.IsOutdated) ? PackageStatus.Outdated : PackageStatus.None)
        | (Usages.Any(u => u.IsDeprecated) ? PackageStatus.Deprecated : PackageStatus.None)
        | (Usages.Any(u => u.IsVulnerable) ? PackageStatus.Vulnerable : PackageStatus.None)
        | (Package.HasVersionMismatch ? PackageStatus.Mismatch : PackageStatus.None);
}
