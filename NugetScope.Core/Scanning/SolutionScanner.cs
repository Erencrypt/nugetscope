using NugetScope.Core.Packages;
using NugetScope.Core.Solutions;

namespace NugetScope.Core.Scanning;

/// <summary>Reads a solution and scans its packages. Shared entry point for the TUI and a future CI mode.</summary>
public sealed class SolutionScanner(ISolutionReader solutionReader, IPackageSource packageSource)
{
    /// <exception cref="SolutionLoadException">The solution file could not be read.</exception>
    /// <exception cref="PackageSourceException">Package data could not be obtained at all.</exception>
    public async Task<SolutionScan> ScanAsync(
        string solutionPath,
        PackageScanOptions options,
        IProgress<ScanStage>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(ScanStage.ReadingSolution);
        SolutionInfo solution = await solutionReader.ReadAsync(solutionPath, cancellationToken).ConfigureAwait(false);

        progress?.Report(ScanStage.ScanningPackages);
        PackageScanResult packages = await packageSource.ScanAsync(solution, options, cancellationToken).ConfigureAwait(false);

        return new SolutionScan(solution, packages);
    }
}
