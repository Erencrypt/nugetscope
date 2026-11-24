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

    /// <summary>More than one resolved version is in use across the solution.</summary>
    public bool HasVersionMismatch => ResolvedVersions.Count > 1;
}
