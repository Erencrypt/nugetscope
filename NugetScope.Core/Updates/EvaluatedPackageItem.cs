namespace NugetScope.Core.Updates;

/// <summary>A <c>PackageReference</c> or <c>PackageVersion</c> item after MSBuild evaluation.</summary>
/// <param name="Version">Evaluated <c>Version</c> metadata; null when absent (e.g. a CPM reference).</param>
/// <param name="DefiningFile">The file whose XML declares the item (csproj, props or targets).</param>
public sealed record EvaluatedPackageItem(string Id, string? Version, string? VersionOverride, string DefiningFile);
