namespace NugetScope.Core.Unused;

/// <summary>A <c>PackageReference</c> with the asset metadata that decides whether its code is meant to be used.</summary>
public sealed record DeclaredReference(string Id, string? PrivateAssets, string? IncludeAssets, string? ExcludeAssets)
{
    /// <summary>
    /// Development-only references (<c>PrivateAssets="all"</c>: analyzers, source link, design-time tools) and
    /// references that exclude compile assets aren't meant to be used from code.
    /// </summary>
    public bool ProvidesCompileAssets =>
        !Has(PrivateAssets, "all")
        && (IncludeAssets is null || Has(IncludeAssets, "all") || Has(IncludeAssets, "compile"))
        && !Has(ExcludeAssets, "all") && !Has(ExcludeAssets, "compile");

    private static bool Has(string? assets, string name) =>
        assets?.Split(';', StringSplitOptions.TrimEntries).Contains(name, StringComparer.OrdinalIgnoreCase) == true;
}
