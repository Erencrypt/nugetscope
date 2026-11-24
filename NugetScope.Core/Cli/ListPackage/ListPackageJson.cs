namespace NugetScope.Core.Cli.ListPackage;

// DTOs for `dotnet list package --format json --output-version 1`.
// Kept together because they only describe one wire format. Property matching is case-insensitive
// (the CLI emits "advisoryurl" in lowercase).

internal sealed class ListPackageJson
{
    public int Version { get; init; }

    public List<ProblemJson>? Problems { get; init; }

    public List<ProjectJson>? Projects { get; init; }
}

internal sealed class ProblemJson
{
    public string? Project { get; init; }

    public string? Level { get; init; }

    public string? Text { get; init; }
}

internal sealed class ProjectJson
{
    public required string Path { get; init; }

    public List<FrameworkJson>? Frameworks { get; init; }
}

internal sealed class FrameworkJson
{
    public required string Framework { get; init; }

    public List<PackageJson>? TopLevelPackages { get; init; }

    public List<PackageJson>? TransitivePackages { get; init; }
}

internal sealed class PackageJson
{
    public required string Id { get; init; }

    public string? RequestedVersion { get; init; }

    public string? ResolvedVersion { get; init; }

    public string? LatestVersion { get; init; }

    /// <summary>Emitted as the string "true", not a JSON boolean.</summary>
    public string? AutoReferenced { get; init; }

    public List<string>? DeprecationReasons { get; init; }

    public AlternativePackageJson? AlternativePackage { get; init; }

    public List<VulnerabilityJson>? Vulnerabilities { get; init; }
}

internal sealed class AlternativePackageJson
{
    public string? Id { get; init; }

    public string? VersionRange { get; init; }
}

internal sealed class VulnerabilityJson
{
    public string? Severity { get; init; }

    public string? AdvisoryUrl { get; init; }
}
