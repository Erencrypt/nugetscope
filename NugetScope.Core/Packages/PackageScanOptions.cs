namespace NugetScope.Core.Packages;

/// <param name="IncludeTransitive">Also list transitive packages.</param>
/// <param name="Restore">Restore the solution first. Disable only when it is known to be restored.</param>
/// <param name="Checks">Status checks to run; each one queries the package feeds.</param>
public sealed record PackageScanOptions(
    bool IncludeTransitive = false,
    bool Restore = true,
    PackageChecks Checks = PackageChecks.All)
{
    public static PackageScanOptions Default { get; } = new();
}
