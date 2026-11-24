using NugetScope.Core.Scanning;
using NugetScope.Core.Solutions;

namespace NugetScope.Core.Packages;

/// <summary>Supplies package data for a solution. Implemented via the dotnet CLI today; swappable for NuGet.Protocol later.</summary>
public interface IPackageSource
{
    /// <exception cref="PackageSourceException">The data source could not run or returned unreadable output.</exception>
    Task<PackageScanResult> ScanAsync(
        SolutionInfo solution,
        PackageScanOptions options,
        IProgress<ScanStage>? progress = null,
        CancellationToken cancellationToken = default);
}
