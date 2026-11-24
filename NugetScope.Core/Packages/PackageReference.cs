namespace NugetScope.Core.Packages;

/// <summary>One package as seen by one project for one target framework.</summary>
public sealed record PackageReference
{
    public required string Id { get; init; }

    public required string TargetFramework { get; init; }

    /// <summary>Version as written (may be floating like <c>3.*</c> or a range like <c>[2.0,)</c>). Null for transitive packages.</summary>
    public string? RequestedVersion { get; init; }

    public string? ResolvedVersion { get; init; }

    public bool IsTransitive { get; init; }

    /// <summary>Implicitly added by the SDK (e.g. NETStandard.Library); not user-editable.</summary>
    public bool IsAutoReferenced { get; init; }

    /// <summary>Only set when the scan included outdated checks.</summary>
    public string? LatestVersion { get; init; }

    /// <summary>Only set when the scan included deprecation checks.</summary>
    public PackageDeprecation? Deprecation { get; init; }

    /// <summary>Empty unless the scan included vulnerability checks.</summary>
    public IReadOnlyList<PackageVulnerability> Vulnerabilities { get; init; } = [];
}
