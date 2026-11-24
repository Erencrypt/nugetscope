namespace NugetScope.Core.Updates;

/// <summary>The package items of one evaluated project (for one target framework).</summary>
public sealed record ProjectEvaluation(
    bool UsesCentralPackageManagement,
    IReadOnlyList<EvaluatedPackageItem> PackageReferences,
    IReadOnlyList<EvaluatedPackageItem> PackageVersions)
{
    /// <summary>Where this project's version of <paramref name="packageId"/> is written; null when it isn't referenced or has no version.</summary>
    public VersionDeclaration? FindDeclaration(string packageId)
    {
        // Later items win in MSBuild, so take the last match.
        EvaluatedPackageItem? reference = PackageReferences.LastOrDefault(i => IsPackage(i, packageId));

        if (reference is null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(reference.VersionOverride))
        {
            return new VersionDeclaration(reference.DefiningFile, VersionDeclarationKind.VersionOverride);
        }

        if (UsesCentralPackageManagement)
        {
            EvaluatedPackageItem? central = PackageVersions.LastOrDefault(i => IsPackage(i, packageId));
            return central is null ? null : new VersionDeclaration(central.DefiningFile, VersionDeclarationKind.PackageVersion);
        }

        return string.IsNullOrEmpty(reference.Version)
            ? null
            : new VersionDeclaration(reference.DefiningFile, VersionDeclarationKind.PackageReference);
    }

    private static bool IsPackage(EvaluatedPackageItem item, string packageId) =>
        string.Equals(item.Id, packageId, StringComparison.OrdinalIgnoreCase);
}
